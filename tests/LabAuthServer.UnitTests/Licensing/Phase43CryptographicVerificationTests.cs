using System.Security.Cryptography;
using System.Text;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.3 cryptographic verification tests: RSA-PSS / SHA-256 over exact payload bytes,
/// trusted-key selection, key rotation, key-size policy, signature encoding and fail-closed
/// behaviour. All keys are ephemeral and generated per test; no private key is retained.
/// </summary>
public sealed class Phase43CryptographicVerificationTests
{
    // ---------- Successful verification ----------

    [Fact]
    public void Rsa3072_Pss_Sha256_ValidSignature_Verifies()
    {
        using var rsa = RSA.Create(3072);
        var payload = "license-payload"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.True(outcome.Succeeded);
        Assert.Null(outcome.Failure);
    }

    [Fact]
    public void ValidSignature_WithKeyA_Verifies()
    {
        using var keyA = RSA.Create(3072);
        var payload = "payload-a"u8.ToArray();
        var signature = keyA.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-a", keyA);
        Assert.True(new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-a", signature).Succeeded);
    }

    [Fact]
    public void ValidSignature_WithKeyB_Verifies()
    {
        using var keyB = RSA.Create(3072);
        var payload = "payload-b"u8.ToArray();
        var signature = keyB.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-b", keyB);
        Assert.True(new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-b", signature).Succeeded);
    }

    [Fact]
    public void MultipleTrustedKeys_WorkIndependently()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);
        using var keyC = RSA.Create(3072);
        var payload = "shared-payload"u8.ToArray();

        using var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add("key-a", keyA);
        provider.Add("key-b", keyB);
        provider.Add("key-c", keyC);

        var verifier = new RsaPssLicenseSignatureVerifier(provider);

        foreach (var (id, key) in new[] { ("key-a", keyA), ("key-b", keyB), ("key-c", keyC) })
        {
            var signature = key.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            Assert.True(verifier.Verify(payload, LicenseConstants.RsaPssSha256Algorithm, id, signature).Succeeded,
                $"license signed by {id} must verify");
        }
    }

    // ---------- Payload integrity ----------

    [Fact]
    public void ModifiedPayload_Fails()
    {
        using var rsa = RSA.Create(3072);
        var payload = "payload-bytes"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        var modified = (byte[])payload.Clone();
        modified[0] ^= 0x01;

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(modified, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void SingleBytePayloadModification_Fails()
    {
        using var rsa = RSA.Create(3072);
        var payload = new byte[32];
        for (var i = 0; i < payload.Length; i++) payload[i] = (byte)i;
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        for (var index = 0; index < payload.Length; index++)
        {
            var mutated = (byte[])payload.Clone();
            mutated[index] ^= 0xFF;
            using var provider = ProviderWith("key-2026", rsa);
            var outcome = new RsaPssLicenseSignatureVerifier(provider)
                .Verify(mutated, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);
            Assert.False(outcome.Succeeded);
        }
    }

    [Fact]
    public void SignatureFromAnotherPayload_Fails()
    {
        using var rsa = RSA.Create(3072);
        var signedPayload = "signed-payload"u8.ToArray();
        var otherPayload = "other-payload"u8.ToArray();
        var signature = rsa.SignData(signedPayload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(otherPayload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
    }

    [Fact]
    public void EmptyPayload_IsVerifiedAgainstExactBytes()
    {
        using var rsa = RSA.Create(3072);
        var payload = Array.Empty<byte>();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        Assert.True(new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature).Succeeded);
    }

    // ---------- Algorithm ----------

    [Fact]
    public void ApprovedAlgorithm_Succeeds()
    {
        using var rsa = RSA.Create(3072);
        var payload = "approved"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        Assert.True(new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature).Succeeded);
    }

    [Theory]
    [InlineData("RSA-PKCS1-SHA256")]
    [InlineData("RSA-PSS-SHA1")]
    [InlineData("RSA-PSS-SHA384")]
    [InlineData("ECDSA-P256-SHA256")]
    [InlineData("ED25519")]
    [InlineData("HMACSHA256")]
    [InlineData("rsa-pss-sha256")]
    [InlineData("")]
    public void UnsupportedAlgorithm_FailsClosed(string algorithm)
    {
        using var rsa = RSA.Create(3072);
        var payload = "payload"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.AlgorithmUnsupported, outcome.Failure);
    }

    [Fact]
    public void Pkcs1Signature_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        var payload = "pkcs1-payload"u8.ToArray();
        var pkcs1Signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", pkcs1Signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void Sha384Signature_IsRejected()
    {
        using var rsa = RSA.Create(3072);
        var payload = "sha384-payload"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    // ---------- Key handling ----------

    [Fact]
    public void UnknownKeyId_FailsClosed()
    {
        using var rsa = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(new byte[] { 1 }, LicenseConstants.RsaPssSha256Algorithm, "unknown-key", new byte[] { 1 });

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.KeyUntrusted, outcome.Failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrEmptyKeyId_FailsClosed(string keyId)
    {
        using var rsa = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(new byte[] { 1 }, LicenseConstants.RsaPssSha256Algorithm, keyId, new byte[] { 1 });

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.KeyUntrusted, outcome.Failure);
    }

    [Fact]
    public void WrongTrustedPublicKey_Fails()
    {
        using var signingKey = RSA.Create(3072);
        using var otherKey = RSA.Create(3072);
        var payload = "payload"u8.ToArray();
        var signature = signingKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", otherKey);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    [InlineData(4096)]
    public void ApprovedKeySizes_VerifyCorrectly(int bits)
    {
        using var rsa = RSA.Create(bits);
        var payload = "size-payload"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-2026", rsa);
        Assert.True(new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", signature).Succeeded);
    }

    [Fact]
    public void WeakRsaKeyBelowMinimum_IsRejected()
    {
        using var rsa = RSA.Create(1024);
        var payload = "weak"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = ProviderWith("key-weak", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-weak", signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.InvalidConfiguration, outcome.Failure);
    }

    // ---------- Signature encoding (parser level) ----------

    [Fact]
    public void MalformedBase64Signature_IsRejected()
    {
        var parser = new JsonLicenseDocumentParser();
        var outcome = parser.Parse(BuildContainer(signature: "not-base64!!!"));
        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    [Fact]
    public void EmptySignature_IsRejected()
    {
        var parser = new JsonLicenseDocumentParser();
        var outcome = parser.Parse(BuildContainer(signature: string.Empty));
        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    [Fact]
    public void WhitespaceInSignature_IsRejected()
    {
        var parser = new JsonLicenseDocumentParser();
        var outcome = parser.Parse(BuildContainer(signature: "AQ ID"));
        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    // ---------- Provider security (F-3) ----------

    [Fact]
    public void Provider_DiscardsPrivateKeyMaterial()
    {
        using var privateKey = RSA.Create(3072);
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add("key-2026", privateKey);

        var stored = provider.TryGetPublicKey("key-2026");
        Assert.NotNull(stored);
        Assert.ThrowsAny<CryptographicException>(() => stored!.ExportParameters(true));
        provider.Dispose();
    }

    [Fact]
    public void Provider_RejectsDuplicateKeyId()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", keyA);
        Assert.Throws<InvalidOperationException>(() => provider.Add("key-2026", keyB));
    }

    [Fact]
    public void Provider_UnknownKeyLookupReturnsNull()
    {
        using var rsa = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", rsa);
        Assert.Null(provider.TryGetPublicKey("missing"));
        Assert.Null(provider.TryGetPublicKey(string.Empty));
    }

    [Fact]
    public void Provider_AddedKeyDoesNotExposePrivateMaterialThroughVerification()
    {
        using var signingKey = RSA.Create(3072);
        var payload = "payload"u8.ToArray();
        var signature = signingKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        using var provider = new InMemoryTrustedLicenseKeyProvider();
        // Add the signing key, which holds private material; the provider must reduce it to public-only.
        provider.Add("key-2026", signingKey);

        var stored = provider.TryGetPublicKey("key-2026");
        Assert.NotNull(stored);
        Assert.ThrowsAny<CryptographicException>(() => stored!.ExportParameters(true));
    }

    // ---------- Error handling ----------

    [Fact]
    public void NullPayloadOrSignature_Throws()
    {
        using var rsa = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", rsa);
        var verifier = new RsaPssLicenseSignatureVerifier(provider);

        Assert.Throws<ArgumentNullException>(() =>
            verifier.Verify(null!, LicenseConstants.RsaPssSha256Algorithm, "key-2026", new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() =>
            verifier.Verify(new byte[] { 1 }, LicenseConstants.RsaPssSha256Algorithm, "key-2026", null!));
    }

    [Fact]
    public void CryptographicFailure_ReturnsStructuredFailureNotSuccess()
    {
        using var rsa = RSA.Create(3072);
        var payload = "payload"u8.ToArray();
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        // Truncated signature bytes force a cryptographic failure inside the verifier.
        var truncated = signature[..(signature.Length / 2)];

        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(payload, LicenseConstants.RsaPssSha256Algorithm, "key-2026", truncated);

        Assert.False(outcome.Succeeded);
        Assert.NotNull(outcome.Failure);
    }

    [Fact]
    public void FailureOutcomes_CarryNoKeyMaterial()
    {
        using var rsa = RSA.Create(3072);
        using var provider = ProviderWith("key-2026", rsa);
        var outcome = new RsaPssLicenseSignatureVerifier(provider)
            .Verify(new byte[] { 1 }, LicenseConstants.RsaPssSha256Algorithm, "missing", new byte[] { 1 });

        Assert.False(outcome.Succeeded);
        // The reason code is a fixed enum value and carries no key or signature material.
        Assert.IsType<LicenseValidationReason>(outcome.Failure);
    }

    // ---------- Helpers ----------

    private static InMemoryTrustedLicenseKeyProvider ProviderWith(string keyId, RSA source)
    {
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(keyId, source);
        return provider;
    }

    private static byte[] BuildContainer(string signature)
    {
        var payload = new Dictionary<string, object?>
        {
            ["licenseVersion"] = 1,
            ["licenseId"] = "license-0001",
            ["product"] = LicenseConstants.Product,
            ["edition"] = "Professional",
            ["issuedAt"] = "2026-09-11T00:00:00Z",
            ["expiresAt"] = "2027-09-11T00:00:00Z",
            ["features"] = new[] { "auth.ldap" },
            ["limits"] = new Dictionary<string, int> { ["max.users"] = 100 }
        };

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var container = new Dictionary<string, object?>
        {
            ["payload"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)),
            ["algorithm"] = LicenseConstants.RsaPssSha256Algorithm,
            ["keyId"] = "key-2026",
            ["signature"] = signature
        };
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(container);
    }
}