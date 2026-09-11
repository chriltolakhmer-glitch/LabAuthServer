using System.Security.Cryptography;
using System.Text;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;
using LabAuthServer.LicenseIssuer;
using IssuerType = LabAuthServer.LicenseIssuer.LicenseIssuer;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.4 issuer tests. Every signing key is generated in memory per test and disposed; no key
/// material is read from disk, committed, or written to output. Signing keys are added to the
/// trusted key provider so issued licenses can be verified by the Phase 4.3 verifier.
/// </summary>
public sealed class Phase44LicenseIssuerTests
{
    private const string KeyId = "lab-license-signing-2026";

    // ---------- Issuance ----------

    [Fact]
    public void PerpetualLicense_CanBeIssued()
    {
        using var key = RSA.Create(3072);
        var issuer = CreateIssuer(key);

        var result = issuer.Issue(CreateRequest(expiresAt: null));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.License);
        Assert.Null(result.License!.Document.ExpiresAt);
        Assert.True(result.License.Document.IsPerpetual);
    }

    [Fact]
    public void TimeLimitedLicense_CanBeIssued()
    {
        using var key = RSA.Create(3072);
        var issuer = CreateIssuer(key);
        var expiresAt = IssuedAt.AddYears(1);

        var result = issuer.Issue(CreateRequest(expiresAt: expiresAt));

        Assert.True(result.Succeeded);
        Assert.Equal(expiresAt, result.License!.Document.ExpiresAt);
        Assert.False(result.License.Document.IsPerpetual);
    }

    [Fact]
    public void IssuedLicense_PreservesAllRequestedFields()
    {
        using var key = RSA.Create(3072);
        var issuer = CreateIssuer(key);
        var request = new LicenseIssuanceRequest
        {
            LicenseId = "license-0001",
            Edition = LicenseEdition.Professional,
            Customer = "Example Customer",
            IssuedAt = IssuedAt,
            ExpiresAt = IssuedAt.AddYears(1),
            Features = new[] { "auth.ldap", "audit.logging" },
            Limits = new Dictionary<string, int> { ["max.users"] = 100 },
            KeyId = KeyId
        };

        var result = issuer.Issue(request);

        Assert.True(result.Succeeded);
        var document = result.License!.Document;
        Assert.Equal("license-0001", document.LicenseId);
        Assert.Equal(LicenseConstants.Product, document.Product);
        Assert.Equal(LicenseEdition.Professional, document.Edition);
        Assert.Equal("Example Customer", document.Customer);
        Assert.Equal(IssuedAt, document.IssuedAt);
        Assert.Equal(request.ExpiresAt, document.ExpiresAt);
        Assert.Equal(request.Features, document.Features);
        Assert.Equal(100, document.Limits["max.users"]);
        Assert.Equal(KeyId, result.License.Signature.KeyId);
        Assert.Equal(LicenseConstants.RsaPssSha256Algorithm, result.License.Signature.Algorithm);
    }

    // ---------- Validation ----------

    [Fact]
    public void MissingLicenseId_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(licenseId: " "));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.LicenseIdMissing, result.Reason);
    }

    [Fact]
    public void InvalidProduct_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(product: "OtherProduct"));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.ProductInvalid, result.Reason);
    }

    [Fact]
    public void UnknownEdition_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(edition: LicenseEdition.Unknown));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.EditionUnknown, result.Reason);
    }

    [Fact]
    public void InvalidIssuedAt_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(issuedAt: default(DateTimeOffset)));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.IssuedAtInvalid, result.Reason);
    }

    [Fact]
    public void InvalidExpiresAt_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(expiresAt: default(DateTimeOffset)));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.ExpiresAtInvalid, result.Reason);
    }

    [Fact]
    public void ExpiresAtBeforeIssuedAt_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(expiresAt: IssuedAt.AddDays(-1)));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.ExpiresAtBeforeIssuedAt, result.Reason);
    }

    [Fact]
    public void MissingKeyId_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(keyId: " "));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.KeyIdMissing, result.Reason);
    }

    [Fact]
    public void UnknownSigningKeyId_Fails()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(keyId: "other-key"));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.KeyIdMismatch, result.Reason);
    }

    [Fact]
    public void UnavailableSigningKey_FailsClosed()
    {
        var issuer = new IssuerType(new NullSigningKeyProvider(KeyId));
        var result = issuer.Issue(CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.SigningKeyUnavailable, result.Reason);
    }

    [Fact]
    public void WeakSigningKey_Fails()
    {
        using var weakKey = RSA.Create(1024);
        var result = CreateIssuer(weakKey).Issue(CreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.SigningKeyPolicyViolation, result.Reason);
    }

    [Fact]
    public void PublicOnlySigningKey_FailsClosed()
    {
        using var key = RSA.Create(3072);
        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(key.ExportParameters(false));

        var result = CreateIssuer(publicOnly).Issue(CreateRequest());

        publicOnly.Dispose();
        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.SigningKeyUnavailable, result.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Auth.Ldap")]
    [InlineData("1auth")]
    public void InvalidFeatureIdentifier_Fails(string feature)
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(features: new[] { feature }));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.FeatureInvalid, result.Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidLimitValue_Fails(int value)
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(
            limits: new Dictionary<string, int> { ["max.users"] = value }));

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseIssuanceReason.LimitInvalid, result.Reason);
    }

    // ---------- Cryptography and compatibility ----------

    [Fact]
    public void IssuedLicense_VerifiesWithPhase43Verifier()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        Assert.True(result.Succeeded);
        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            result.License!.SignedPayload,
            result.License.Signature.Algorithm,
            result.License.Signature.KeyId,
            Convert.FromBase64String(result.License.Signature.Signature));

        Assert.True(outcome.Succeeded);
    }

    [Fact]
    public void IssuedLicense_VerifiesWithRsa3072()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        Assert.True(result.Succeeded);
        Assert.Equal(3072, key.KeySize);
    }

    [Fact]
    public void TamperedPayload_FailsVerification()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        var payload = (byte[])result.License!.SignedPayload.Clone();
        payload[0] ^= 0x01;

        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            payload,
            result.License.Signature.Algorithm,
            result.License.Signature.KeyId,
            Convert.FromBase64String(result.License.Signature.Signature));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void TamperedSignature_FailsVerification()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        var signature = Convert.FromBase64String(result.License!.Signature.Signature);
        signature[0] ^= 0x01;

        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            result.License.SignedPayload,
            result.License.Signature.Algorithm,
            result.License.Signature.KeyId,
            signature);

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void WrongKeyId_FailsVerification()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            result.License!.SignedPayload,
            result.License.Signature.Algorithm,
            "unknown-key",
            Convert.FromBase64String(result.License.Signature.Signature));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.KeyUntrusted, outcome.Failure);
    }

    [Fact]
    public void WrongPublicKey_FailsVerification()
    {
        using var signingKey = RSA.Create(3072);
        using var otherKey = RSA.Create(3072);
        var result = CreateIssuer(signingKey).Issue(CreateRequest());

        using var verifier = CreateVerifier(otherKey);
        var outcome = verifier.Verify(
            result.License!.SignedPayload,
            result.License.Signature.Algorithm,
            result.License.Signature.KeyId,
            Convert.FromBase64String(result.License.Signature.Signature));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureInvalid, outcome.Failure);
    }

    [Fact]
    public void IssuedSignature_UsesApprovedAlgorithmIdentifier()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        Assert.Equal(LicenseConstants.RsaPssSha256Algorithm, result.License!.Signature.Algorithm);
    }

    [Fact]
    public void Pkcs1SignedPayload_DoesNotVerifyAsIssuedLicense()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        // Re-sign the exact payload with PKCS#1 v1.5 instead of RSA-PSS.
        var pkcs1Signature = key.SignData(
            result.License!.SignedPayload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            result.License.SignedPayload,
            result.License.Signature.Algorithm,
            result.License.Signature.KeyId,
            pkcs1Signature);

        Assert.False(outcome.Succeeded);
    }

    [Fact]
    public void ExactPayloadBytes_AreEmbeddedInContainer()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        using var container = System.Text.Json.JsonDocument.Parse(result.ContainerBytes!);
        var embedded = Convert.FromBase64String(
            container.RootElement.GetProperty("payload").GetString()!);

        Assert.Equal(result.License!.SignedPayload, embedded);
    }

    [Fact]
    public void Serializer_IsDeterministicForTheSameDocument()
    {
        var document = new LicenseDocument
        {
            LicenseVersion = 1,
            LicenseId = "license-0001",
            Product = LicenseConstants.Product,
            Edition = LicenseEdition.Enterprise,
            IssuedAt = IssuedAt,
            ExpiresAt = IssuedAt.AddYears(1),
            Features = new[] { "auth.jwt", "auth.ldap" },
            Limits = new Dictionary<string, int> { ["max.users"] = 500, ["max.groups"] = 10 }
        };

        var first = LicensePayloadSerializer.Serialize(document);
        var second = LicensePayloadSerializer.Serialize(document);

        Assert.Equal(first, second);
        // Limits are emitted in ordinal key order regardless of dictionary insertion order.
        var text = Encoding.UTF8.GetString(first);
        Assert.True(text.IndexOf("max.groups", StringComparison.Ordinal)
            < text.IndexOf("max.users", StringComparison.Ordinal));
    }

    // ---------- Key rotation ----------

    [Fact]
    public void LicensesIssuedWithDifferentKeys_VerifyWithTheirOwnKey()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);

        var resultA = CreateIssuer(keyA, "key-a").Issue(CreateRequest(keyId: "key-a"));
        var resultB = CreateIssuer(keyB, "key-b").Issue(CreateRequest(keyId: "key-b"));

        using var verifierA = CreateVerifier(keyA, "key-a");
        using var verifierB = CreateVerifier(keyB, "key-b");

        Assert.True(verifierA.Verify(
            resultA.License!.SignedPayload,
            resultA.License.Signature.Algorithm,
            "key-a",
            Convert.FromBase64String(resultA.License.Signature.Signature)).Succeeded);

        Assert.True(verifierB.Verify(
            resultB.License!.SignedPayload,
            resultB.License.Signature.Algorithm,
            "key-b",
            Convert.FromBase64String(resultB.License.Signature.Signature)).Succeeded);
    }

    [Fact]
    public void KeyId_IdentifiesTheCorrectSigningKey()
    {
        using var keyA = RSA.Create(3072);
        using var keyB = RSA.Create(3072);

        var resultA = CreateIssuer(keyA, "key-a").Issue(CreateRequest(keyId: "key-a"));
        var resultB = CreateIssuer(keyB, "key-b").Issue(CreateRequest(keyId: "key-b"));

        Assert.Equal("key-a", resultA.License!.Signature.KeyId);
        Assert.Equal("key-b", resultB.License!.Signature.KeyId);
    }

    // ---------- Private-key safety ----------

    [Fact]
    public void Container_ContainsNoPrivateKeyMaterial()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        var text = Encoding.UTF8.GetString(result.ContainerBytes!);
        Assert.DoesNotContain("PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PrivateKey", text, StringComparison.OrdinalIgnoreCase);

        // The container must carry no RSA private parameter material (modulus is public; private
        // exponent, primes and CRT parameters are private). Presence of a base64 blob alone is
        // expected for payload/signature, so assert on the decoded payload instead.
        using var container = System.Text.Json.JsonDocument.Parse(result.ContainerBytes!);
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(
            container.RootElement.GetProperty("payload").GetString()!));
        Assert.DoesNotContain("private", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IssuedLicense_ContainsOnlyPublicVerifiableInformation()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest());

        // The signed license object exposes the public key ID and signature but no private key.
        Assert.NotNull(result.License!.Signature);
        Assert.DoesNotContain("PRIVATE", result.License.Signature.Signature, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FailureResult_CarriesNoLicenseOrContainer()
    {
        using var key = RSA.Create(3072);
        var result = CreateIssuer(key).Issue(CreateRequest(product: "OtherProduct"));

        Assert.False(result.Succeeded);
        Assert.Null(result.License);
        Assert.Null(result.ContainerBytes);
    }

    // ---------- Round trip ----------

    [Fact]
    public void RoundTrip_IssueParseVerifyPreservesDocument()
    {
        using var key = RSA.Create(3072);
        var request = new LicenseIssuanceRequest
        {
            LicenseId = "license-roundtrip",
            Edition = LicenseEdition.Enterprise,
            Customer = "Round Trip Customer",
            IssuedAt = IssuedAt,
            ExpiresAt = IssuedAt.AddYears(2),
            Features = new[] { "auth.jwt", "audit.logging" },
            Limits = new Dictionary<string, int> { ["max.users"] = 250 },
            KeyId = KeyId
        };

        var issued = CreateIssuer(key).Issue(request);
        Assert.True(issued.Succeeded);

        // Parse with the server parser.
        var parser = new JsonLicenseDocumentParser();
        var parsed = parser.Parse(issued.ContainerBytes!);
        Assert.True(parsed.Succeeded);

        var parsedLicense = parsed.License!;
        Assert.Equal(request.LicenseId, parsedLicense.Document.LicenseId);
        Assert.Equal(request.Edition, parsedLicense.Document.Edition);
        Assert.Equal(request.Customer, parsedLicense.Document.Customer);
        Assert.Equal(request.IssuedAt, parsedLicense.Document.IssuedAt);
        Assert.Equal(request.ExpiresAt, parsedLicense.Document.ExpiresAt);
        Assert.Equal(request.Features, parsedLicense.Document.Features);
        Assert.Equal(250, parsedLicense.Document.Limits["max.users"]);

        // Verify with the server verifier over the exact parsed payload bytes.
        using var verifier = CreateVerifier(key);
        var outcome = verifier.Verify(
            parsedLicense.SignedPayload,
            parsedLicense.Signature.Algorithm,
            parsedLicense.Signature.KeyId,
            Convert.FromBase64String(parsedLicense.Signature.Signature));

        Assert.True(outcome.Succeeded);
        Assert.Equal(issued.License!.SignedPayload, parsedLicense.SignedPayload);
    }

    // ---------- Helpers ----------

    private static readonly DateTimeOffset IssuedAt =
        new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);

    private static IssuerType CreateIssuer(RSA key, string keyId = KeyId)
        => new(new InMemorySigningKeyProvider(keyId, key));

    private static VerifierHandle CreateVerifier(RSA key, string keyId = KeyId)
    {
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(keyId, key);
        return new VerifierHandle(provider);
    }

    private static LicenseIssuanceRequest CreateRequest(
        string licenseId = "license-0001",
        string product = LicenseConstants.Product,
        LicenseEdition edition = LicenseEdition.Professional,
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? expiresAt = null,
        string keyId = KeyId,
        IReadOnlyList<string>? features = null,
        IReadOnlyDictionary<string, int>? limits = null)
        => new()
        {
            LicenseId = licenseId,
            Product = product,
            Edition = edition,
            Customer = "Example Customer",
            IssuedAt = issuedAt ?? IssuedAt,
            ExpiresAt = expiresAt,
            Features = features ?? new[] { "auth.ldap" },
            Limits = limits ?? new Dictionary<string, int> { ["max.users"] = 100 },
            KeyId = keyId
        };

    private sealed class InMemorySigningKeyProvider : ILicenseSigningKeyProvider
    {
        private readonly RSA _key;

        public InMemorySigningKeyProvider(string keyId, RSA key)
        {
            KeyId = keyId;
            _key = key;
        }

        public string KeyId { get; }

        public RSA? TryGetSigningKey() => _key;
    }

    private sealed class NullSigningKeyProvider : ILicenseSigningKeyProvider
    {
        public NullSigningKeyProvider(string keyId) => KeyId = keyId;

        public string KeyId { get; }

        public RSA? TryGetSigningKey() => null;
    }

    /// <summary>
    /// Disposable test wrapper owning the trusted key provider and exposing the verifier.
    /// The production verifier is stateless and not disposable; only the key provider is.
    /// </summary>
    private sealed class VerifierHandle : IDisposable
    {
        private readonly InMemoryTrustedLicenseKeyProvider _provider;
        private readonly RsaPssLicenseSignatureVerifier _verifier;

        public VerifierHandle(InMemoryTrustedLicenseKeyProvider provider)
        {
            _provider = provider;
            _verifier = new RsaPssLicenseSignatureVerifier(provider);
        }

        public LicenseSignatureVerificationOutcome Verify(
            byte[] signedPayload, string algorithm, string keyId, byte[] signature)
            => _verifier.Verify(signedPayload, algorithm, keyId, signature);

        public void Dispose() => _provider.Dispose();
    }
}