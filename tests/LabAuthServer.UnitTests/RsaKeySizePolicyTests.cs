using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class RsaKeySizePolicyTests
{
    [Theory]
    [InlineData(1024, false)]
    [InlineData(2048, true)]
    [InlineData(3072, true)]
    [InlineData(4096, true)]
    [InlineData(4160, false)]
    public async Task AllKeyLoadingPaths_EnforceRange(int bits, bool accepted)
    {
        using var rsa = RSA.Create(bits);
        var request = new CertificateRequest("CN=SizePolicy.Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
        Assert.False(publicCertificate.HasPrivateKey);

        foreach (var previous in new[] { false, true })
        {
            var options = CreateOptions();
            options.SigningCertificateStoreLocation = "LocalMachine";
            options.SigningCertificateStoreName = "My";
            options.SigningCertificateThumbprint = previous ? new string('A', 40) : certificate.Thumbprint;
            options.PreviousSigningCertificateThumbprint = previous ? certificate.Thumbprint : new string('A', 40);
            var signingProvider = new CertificateSigningKeyProvider(Options.Create(options), (_, _) => certificate);
            var validationProvider = new CertificateSigningKeyProvider(Options.Create(options), (_, _) => publicCertificate);
            var keyId = previous ? "previous" : "active";
            await CheckSigning(signingProvider, keyId, accepted, bits);
            await CheckValidation(validationProvider, keyId, accepted, bits);

            var runtime = new ProtectedSigningKeyProvider(Options.Create(CreateOptions()),
                new[] { new KeyValuePair<string, RSA>("active", rsa), new KeyValuePair<string, RSA>("previous", rsa) });
            await CheckSigning(runtime, keyId, accepted, bits);
            await CheckValidation(runtime, keyId, accepted, bits);
        }
    }

    [Fact]
    public async Task PreviousSigningKey_WithoutOverlapExpiry_IsRejected()
    {
        using var rsa = RSA.Create(2048);
        var options = CreateOptions();
        options.PreviousKeyExpiresAt = null;
        var provider = new ProtectedSigningKeyProvider(Options.Create(options),
            new[] { new KeyValuePair<string, RSA>("previous", rsa) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetKeyAsync("previous").AsTask());
    }

    private static async Task CheckSigning(IProtectedSigningKeyProvider provider, string keyId, bool accepted, int bits)
    {
        if (!accepted)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                (keyId == "active" ? provider.GetActiveKeyAsync() : provider.GetKeyAsync(keyId)).AsTask());
            return;
        }
        using var key = await (keyId == "active" ? provider.GetActiveKeyAsync() : provider.GetKeyAsync(keyId));
        Assert.Equal(bits, key.PrivateKey.KeySize);
    }

    private static async Task CheckValidation(IProtectedSigningKeyProvider provider, string keyId, bool accepted, int bits)
    {
        if (!accepted)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetValidationKeyAsync(keyId).AsTask());
            return;
        }
        using var key = await provider.GetValidationKeyAsync(keyId);
        Assert.Equal(bits, key.PublicKey.KeySize);
        Assert.ThrowsAny<CryptographicException>(() => key.PublicKey.ExportParameters(true));
    }

    private static TokenOptions CreateOptions() => new()
    {
        Issuer = "https://size-tests.example", Audience = "size-tests", SigningAlgorithm = "RS256",
        ActiveKeyId = "active", PreviousKeyId = "previous", PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        AccessTokenLifetime = TimeSpan.FromMinutes(10), ClockSkew = TimeSpan.Zero,
        SigningKeyStoreReference = "runtime-test", MaximumClaimSize = 4096, MaximumTokenSize = 16384
    };
}
