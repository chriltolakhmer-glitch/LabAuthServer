using System.Security.Cryptography;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

public sealed class RsaPssLicenseSignatureVerifierTests
{
    [Fact]
    public void ValidSignature_VerifiesWithoutAnyPrivateKeyDependency()
    {
        using var rsa = RSA.Create(3072);
        var payload = "payload-bytes"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var keyProvider = CreateKeyProvider("key-2026", rsa);
        var verifier = new RsaPssLicenseSignatureVerifier(keyProvider);

        var outcome = verifier.Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.True(outcome.Succeeded);
        Assert.Null(outcome.Failure);
    }

    [Fact]
    public void TamperedPayload_FailsVerification()
    {
        using var rsa = RSA.Create(3072);
        var payload = "payload-bytes"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var keyProvider = CreateKeyProvider("key-2026", rsa);
        var verifier = new RsaPssLicenseSignatureVerifier(keyProvider);

        var outcome = verifier.Verify("other-bytes"u8.ToArray(), LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void UnknownKeyId_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        using var keyProvider = CreateKeyProvider("key-2026", rsa);
        var verifier = new RsaPssLicenseSignatureVerifier(keyProvider);

        var outcome = verifier.Verify(new byte[] { 1 }, LicenseConstants.RsaPssSha256Algorithm, "missing-key", new byte[] { 1 });

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.KeyUntrusted, outcome.Failure);
    }

    [Fact]
    public void UnsupportedAlgorithm_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        using var keyProvider = CreateKeyProvider("key-2026", rsa);
        var verifier = new RsaPssLicenseSignatureVerifier(keyProvider);

        var outcome = verifier.Verify(new byte[] { 1 }, "RSA-PKCS1-SHA1", "key-2026", new byte[] { 1 });

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.AlgorithmUnsupported, outcome.Failure);
    }

    [Fact]
    public void MultipleTrustedKeys_EachVerifiesItsOwnLicense()
    {
        using var primary = RSA.Create(3072);
        using var rotated = RSA.Create(3072);
        var payload = "payload-bytes"u8.ToArray();

        using var keyProvider = new InMemoryTrustedLicenseKeyProvider();
        keyProvider.Add("key-primary", primary);
        keyProvider.Add("key-rotated", rotated);

        var verifier = new RsaPssLicenseSignatureVerifier(keyProvider);

        var signatureFromRotated = rotated.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        Assert.True(verifier.Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-primary",
            primary.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)).Succeeded);
        Assert.True(verifier.Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-rotated",
            signatureFromRotated).Succeeded);
    }

    [Fact]
    public void TrustedKeyProvider_HoldsNoPrivateKey()
    {
        using var rsa = RSA.Create(3072);
        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(rsa.ExportParameters(false));

        Assert.ThrowsAny<CryptographicException>(() => publicOnly.ExportParameters(true));
        publicOnly.Dispose();
    }

    private static InMemoryTrustedLicenseKeyProvider CreateKeyProvider(string keyId, RSA rsa)
    {
        using var publicOnly = RSA.Create();
        publicOnly.ImportParameters(rsa.ExportParameters(false));
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(keyId, publicOnly);
        return provider;
    }
}