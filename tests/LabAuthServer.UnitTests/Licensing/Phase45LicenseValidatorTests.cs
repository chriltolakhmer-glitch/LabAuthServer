using System.Text;
using System.Text.Json;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.5 license validation tests. Covers M-1 (unknown container/payload properties are
/// rejected, duplicates remain rejected), M-2 (strict, non-normalising Base64 handling), the
/// fail-closed result model, time/expiry policy, structural bounds, and the guarantee that the
/// verifier receives the exact signed payload bytes. The validator is driven through a stub
/// signature verifier and a fixed clock so every rule is deterministic.
/// </summary>
public sealed class Phase45LicenseValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------------------------
    // Format: M-1 (unknown JSON properties)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void UnknownContainerProperty_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(
            BuildContainer(mutateContainer: c => c["extra"] = "value"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldUnknown, outcome.Failure);
    }

    [Fact]
    public void UnknownPayloadProperty_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(
            BuildContainer(mutatePayload: p => p["extra"] = "value"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldUnknown, outcome.Failure);
    }

    [Fact]
    public void DuplicateContainerProperty_IsRejected()
    {
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(SerializePayload()));
        var signature = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        var json =
            "{\"payload\":\"" + payloadBase64 + "\"," +
            "\"algorithm\":\"" + LicenseConstants.RsaPssSha256Algorithm + "\"," +
            "\"keyId\":\"key-2026\"," +
            "\"algorithm\":\"" + LicenseConstants.RsaPssSha256Algorithm + "\"," +
            "\"signature\":\"" + signature + "\"}";

        var outcome = new JsonLicenseDocumentParser().Parse(Encoding.UTF8.GetBytes(json));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    [Fact]
    public void DuplicatePayloadProperty_IsRejected()
    {
        var payloadJson = "{\"licenseVersion\":1,\"licenseVersion\":1,\"licenseId\":\"license-0001\"," +
            "\"product\":\"" + LicenseConstants.Product + "\",\"edition\":\"Professional\"," +
            "\"issuedAt\":\"2026-09-11T00:00:00Z\"}";
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        var json =
            "{\"payload\":\"" + payloadBase64 + "\"," +
            "\"algorithm\":\"" + LicenseConstants.RsaPssSha256Algorithm + "\"," +
            "\"keyId\":\"key-2026\"," +
            "\"signature\":\"" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }) + "\"}";

        var outcome = new JsonLicenseDocumentParser().Parse(Encoding.UTF8.GetBytes(json));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    // ---------------------------------------------------------------------------------------
    // Format: M-2 (strict Base64)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void PayloadBase64WithWhitespace_IsRejected()
    {
        var clean = Convert.ToBase64String(Encoding.UTF8.GetBytes(SerializePayload()));
        var spaced = clean.Insert(4, " ");

        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(payloadOverride: spaced));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    [Fact]
    public void PayloadBase64Malformed_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(payloadOverride: "!!!!"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.LicenseMalformed, outcome.Failure);
    }

    [Fact]
    public void EmptyPayloadString_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(payloadOverride: string.Empty));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.FieldMissing, outcome.Failure);
    }

    [Fact]
    public void SignatureBase64WithWhitespace_IsRejected()
    {
        var clean = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var spaced = clean.Insert(2, " ");

        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(signatureOverride: spaced));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    [Fact]
    public void SignatureBase64Malformed_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(signatureOverride: "!!!!"));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    [Fact]
    public void EmptySignatureString_IsRejected()
    {
        var outcome = new JsonLicenseDocumentParser().Parse(BuildContainer(signatureOverride: string.Empty));

        Assert.False(outcome.Succeeded);
        Assert.Equal(LicenseValidationReason.SignatureMalformed, outcome.Failure);
    }

    // ---------------------------------------------------------------------------------------
    // Validator: success path
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ValidLicense_ReturnsValid()
    {
        var result = CreateValidator().Validate(BuildContainer());

        Assert.True(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Valid, result.Status);
        Assert.Equal(LicenseValidationReason.None, result.Reason);
        Assert.NotNull(result.Policy);
    }

    [Fact]
    public void PerpetualLicense_IsAccepted()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["expiresAt"] = null));

        Assert.True(result.IsValid);
        Assert.True(result.Policy!.IsPerpetual);
    }

    // ---------------------------------------------------------------------------------------
    // Validator: fail-closed paths
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void EmptyContent_ReturnsMissing()
    {
        var result = CreateValidator().Validate(ReadOnlySpan<byte>.Empty);

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Missing, result.Status);
        Assert.Equal(LicenseValidationReason.LicenseMissing, result.Reason);
    }

    [Fact]
    public void MalformedContent_ReturnsMalformed()
    {
        var result = CreateValidator().Validate(Encoding.UTF8.GetBytes("{ not json"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Malformed, result.Status);
    }

    [Fact]
    public void UnknownContainerProperty_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutateContainer: c => c["extra"] = "value"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.FieldUnknown, result.Reason);
    }

    [Fact]
    public void UnknownPayloadProperty_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["extra"] = "value"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.FieldUnknown, result.Reason);
    }

    [Fact]
    public void UnsupportedVersion_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["licenseVersion"] = 99));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
    }

    [Fact]
    public void UnknownEdition_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["edition"] = "Ultimate"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
    }

    [Fact]
    public void WrongProduct_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["product"] = "OtherProduct"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.WrongProduct, result.Status);
        Assert.Equal(LicenseValidationReason.ProductMismatch, result.Reason);
    }

    [Fact]
    public void UnsupportedAlgorithm_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutateContainer: c => c["algorithm"] = "RS256"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidSignature, result.Status);
        Assert.Equal(LicenseValidationReason.AlgorithmUnsupported, result.Reason);
    }

    [Fact]
    public void SignatureVerificationFailure_Denies()
    {
        var verifier = new StubSignatureVerifier(
            LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.SignatureInvalid));

        var result = CreateValidator(verifier).Validate(BuildContainer());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidSignature, result.Status);
    }

    [Fact]
    public void UnknownKeyId_Denies()
    {
        var verifier = new StubSignatureVerifier(
            LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.KeyUntrusted));

        var result = CreateValidator(verifier).Validate(BuildContainer());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.UnknownKey, result.Status);
        Assert.Equal(LicenseValidationReason.KeyUntrusted, result.Reason);
    }

    [Fact]
    public void VerifierConfigurationFailure_Denies()
    {
        var verifier = new StubSignatureVerifier(
            LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.InvalidConfiguration));

        var result = CreateValidator(verifier).Validate(BuildContainer());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidConfiguration, result.Status);
    }

    [Fact]
    public void UnexpectedException_Denies()
    {
        var result = CreateValidator(new ThrowingSignatureVerifier()).Validate(BuildContainer());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidConfiguration, result.Status);
    }

    // ---------------------------------------------------------------------------------------
    // Time / expiry
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ExpiredLicense_Denies()
    {
        var result = CreateValidator(clock: new FixedClock(new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero)))
            .Validate(BuildContainer());

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Expired, result.Status);
        Assert.Equal(LicenseValidationReason.Expired, result.Reason);
    }

    [Fact]
    public void IssuedInFutureBeyondSkew_DeniesNotYetValid()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["issuedAt"] = "2026-09-11T13:00:00Z"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.NotYetValid, result.Status);
        Assert.Equal(LicenseValidationReason.NotYetValid, result.Reason);
    }

    [Fact]
    public void IssuedWithinClockSkew_IsAccepted()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["issuedAt"] = "2026-09-11T12:02:00Z"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ExpiryEqualToIssuedAt_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["expiresAt"] = "2026-09-11T00:00:00Z"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.ExpiryInvalid, result.Reason);
    }

    [Fact]
    public void ExpiryBeforeIssuedAt_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["expiresAt"] = "2026-01-01T00:00:00Z"));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.ExpiryInvalid, result.Reason);
    }

    // ---------------------------------------------------------------------------------------
    // Features and limits
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void InvalidFeatureIdentifier_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["features"] = new[] { "Auth.Ldap" }));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.FeatureUnknown, result.Reason);
    }

    [Fact]
    public void FeatureTooLong_Denies()
    {
        var tooLong = new string('a', LicenseValidationPolicy.MaximumFeatureLength + 1);

        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["features"] = new[] { tooLong }));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationReason.FeatureUnknown, result.Reason);
    }

    [Fact]
    public void TooManyFeatures_Denies()
    {
        var many = Enumerable.Range(0, LicenseValidationPolicy.MaximumFeatureCount + 1)
            .Select(i => "feature" + i)
            .ToArray();

        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["features"] = many));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationReason.FeatureUnknown, result.Reason);
    }

    [Fact]
    public void NonPositiveLimit_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["limits"] = new Dictionary<string, int> { ["max.users"] = 0 }));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.LimitOutOfRange, result.Reason);
    }

    [Fact]
    public void NegativeLimit_Denies()
    {
        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["limits"] = new Dictionary<string, int> { ["max.users"] = -1 }));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationReason.LimitOutOfRange, result.Reason);
    }

    [Fact]
    public void LimitKeyTooLong_Denies()
    {
        var key = new string('k', LicenseValidationPolicy.MaximumLimitKeyLength + 1);

        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["limits"] = new Dictionary<string, int> { [key] = 1 }));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationReason.LimitOutOfRange, result.Reason);
    }

    [Fact]
    public void TooManyLimits_Denies()
    {
        var many = Enumerable.Range(0, LicenseValidationPolicy.MaximumLimitCount + 1)
            .ToDictionary(i => "limit" + i, _ => 1);

        var result = CreateValidator().Validate(
            BuildContainer(mutatePayload: p => p["limits"] = many));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationReason.LimitOutOfRange, result.Reason);
    }

    // ---------------------------------------------------------------------------------------
    // Exact-byte verification
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void VerifierReceivesExactSignedPayloadBytesAndEnvelope()
    {
        var expectedPayload = Encoding.UTF8.GetBytes(SerializePayload());
        var verifier = new StubSignatureVerifier();

        var result = CreateValidator(verifier).Validate(BuildContainer());

        Assert.True(result.IsValid);
        Assert.Equal(expectedPayload, verifier.LastSignedPayload);
        Assert.Equal(LicenseConstants.RsaPssSha256Algorithm, verifier.LastAlgorithm);
        Assert.Equal("key-2026", verifier.LastKeyId);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, verifier.LastSignature);
    }

    // ---------------------------------------------------------------------------------------
    // Helpers and fakes
    // ---------------------------------------------------------------------------------------

    private static LicenseValidator CreateValidator(
        ILicenseSignatureVerifier? verifier = null,
        ILicenseClock? clock = null)
        => new(
            new JsonLicenseDocumentParser(),
            verifier ?? new StubSignatureVerifier(),
            clock ?? new FixedClock(Now));

    private static Dictionary<string, object?> BuildPayload(Action<Dictionary<string, object?>>? mutate = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["licenseVersion"] = 1,
            ["licenseId"] = "license-0001",
            ["product"] = LicenseConstants.Product,
            ["edition"] = "Professional",
            ["customer"] = "Example Customer",
            ["issuedAt"] = "2026-09-11T00:00:00Z",
            ["expiresAt"] = "2027-09-11T00:00:00Z",
            ["features"] = new[] { "auth.ldap" },
            ["limits"] = new Dictionary<string, int> { ["max.users"] = 100 }
        };

        mutate?.Invoke(payload);
        return payload;
    }

    private static string SerializePayload(Action<Dictionary<string, object?>>? mutate = null)
        => JsonSerializer.Serialize(BuildPayload(mutate));

    private static byte[] BuildContainer(
        Action<Dictionary<string, object?>>? mutatePayload = null,
        Action<Dictionary<string, object?>>? mutateContainer = null,
        string? payloadOverride = null,
        string? signatureOverride = null)
    {
        var payloadBase64 = payloadOverride
            ?? Convert.ToBase64String(Encoding.UTF8.GetBytes(SerializePayload(mutatePayload)));

        var container = new Dictionary<string, object?>
        {
            ["payload"] = payloadBase64,
            ["algorithm"] = LicenseConstants.RsaPssSha256Algorithm,
            ["keyId"] = "key-2026",
            ["signature"] = signatureOverride ?? Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        };

        mutateContainer?.Invoke(container);
        return JsonSerializer.SerializeToUtf8Bytes(container);
    }

    private sealed class FixedClock : ILicenseClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class StubSignatureVerifier : ILicenseSignatureVerifier
    {
        private readonly LicenseSignatureVerificationOutcome _outcome;

        public StubSignatureVerifier(LicenseSignatureVerificationOutcome? outcome = null)
            => _outcome = outcome ?? LicenseSignatureVerificationOutcome.Success();

        public byte[]? LastSignedPayload { get; private set; }

        public string? LastAlgorithm { get; private set; }

        public string? LastKeyId { get; private set; }

        public byte[]? LastSignature { get; private set; }

        public LicenseSignatureVerificationOutcome Verify(
            byte[] signedPayload,
            string algorithm,
            string keyId,
            byte[] signature)
        {
            LastSignedPayload = signedPayload;
            LastAlgorithm = algorithm;
            LastKeyId = keyId;
            LastSignature = signature;
            return _outcome;
        }
    }

    private sealed class ThrowingSignatureVerifier : ILicenseSignatureVerifier
    {
        public LicenseSignatureVerificationOutcome Verify(
            byte[] signedPayload,
            string algorithm,
            string keyId,
            byte[] signature)
            => throw new InvalidOperationException("simulated verifier failure");
    }
}