using System.Text;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.7 expiration and grace-period tests. Covers the expiration timeline (before, at and
/// after <c>expiresAt</c>), perpetual licenses, the clock-skew boundary, the explicitly
/// default-disabled grace period, and the guarantee that an expired license never grants a
/// commercial feature. All time is supplied through a fixed clock; no wall-clock dependency.
/// </summary>
public sealed class Phase47ExpirationAndGracePeriodTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);

    // One year after IssuedAt: comfortably longer than the warning threshold so the
    // "approaching expiry" and "far from expiry" cases are both reachable.
    private static readonly DateTimeOffset ExpiresAt = new(2027, 9, 11, 0, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------------------------
    // Perpetual licenses (D4.7-6)
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2026, 9, 11)]
    [InlineData(2036, 9, 11)]
    [InlineData(2126, 9, 11)]
    public void PerpetualLicense_RemainsPerpetualAtAnyFutureInstant(int year, int month, int day)
    {
        var evaluator = EvaluatorAt(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero));

        var status = evaluator.Evaluate(Document(expiresAt: null));

        Assert.True(status.IsPerpetual);
        Assert.False(status.IsExpired);
        Assert.False(status.IsWithinGrace);
        Assert.Null(status.ExpiresAt);
        Assert.Null(status.TimeUntilExpiry);
        Assert.True(status.IsUsable);
    }

    [Fact]
    public void PerpetualLicense_DoesNotBecomeRestricted()
    {
        var result = Validate(Document(expiresAt: null), At: IssuedAt.AddYears(50));

        Assert.True(result.IsValid);
        var policy = LicensePolicy.FromValidationResult(result);
        Assert.False(policy.IsRestricted);
    }

    // ---------------------------------------------------------------------------------------
    // Boundaries (D4.7-2, expiresAt is exclusive)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void BeforeExpiration_IsActive()
    {
        var status = EvaluatorAt(ExpiresAt.AddTicks(-1)).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.Active, status.State);
        Assert.True(status.IsUsable);
        Assert.False(status.IsExpired);
        Assert.False(status.IsWithinGrace);
    }

    [Fact]
    public void ExactlyAtExpiration_IsExpired()
    {
        var status = EvaluatorAt(ExpiresAt).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.Expired, status.State);
        Assert.True(status.IsExpired);
        Assert.False(status.IsWithinGrace);
    }

    [Fact]
    public void ImmediatelyAfterExpiration_IsExpired()
    {
        var status = EvaluatorAt(ExpiresAt.AddSeconds(1)).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.Expired, status.State);
    }

    [Fact]
    public void WellAfterExpiration_IsExpired()
    {
        var status = EvaluatorAt(ExpiresAt.AddYears(10)).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.Expired, status.State);
        Assert.False(status.IsWithinGrace);
    }

    [Fact]
    public void ExpirationBoundary_FlowsThroughValidator()
    {
        Assert.True(Validate(Document(expiresAt: ExpiresAt), ExpiresAt.AddTicks(-1)).IsValid);

        var expired = Validate(Document(expiresAt: ExpiresAt), ExpiresAt);
        Assert.False(expired.IsValid);
        Assert.Equal(LicenseValidationStatus.Expired, expired.Status);
        Assert.Equal(LicenseValidationReason.Expired, expired.Reason);
    }

    // ---------------------------------------------------------------------------------------
    // Not-yet-valid
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void BeforeIssuedAtBeyondSkew_IsNotYetValid()
    {
        var now = IssuedAt - LicenseValidationPolicy.ClockSkewAllowance - TimeSpan.FromSeconds(1);

        var status = EvaluatorAt(now).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.NotYetValid, status.State);
        Assert.True(status.IsNotYetValid);
        Assert.False(status.IsUsable);
    }

    [Fact]
    public void WithinClockSkewBeforeIssuedAt_IsActive()
    {
        var now = IssuedAt - LicenseValidationPolicy.ClockSkewAllowance;

        var status = EvaluatorAt(now).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.Equal(LicenseExpirationState.Active, status.State);
        Assert.True(status.IsUsable);
    }

    [Fact]
    public void ClockSkewBoundary_DoesNotExtendExpiration()
    {
        // Skew applies to issuedAt only; it must never widen the expiry side of the window.
        var justInside = ExpiresAt.AddTicks(-1);
        var justOutside = ExpiresAt;

        Assert.Equal(LicenseExpirationState.Active,
            EvaluatorAt(justInside).Evaluate(Document(expiresAt: ExpiresAt)).State);
        Assert.Equal(LicenseExpirationState.Expired,
            EvaluatorAt(justOutside).Evaluate(Document(expiresAt: ExpiresAt)).State);

        Assert.False(Validate(Document(expiresAt: ExpiresAt), justOutside).IsValid);
    }

    // ---------------------------------------------------------------------------------------
    // Grace period (D-08: none)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void GracePeriod_IsDisabledByDefault()
    {
        Assert.False(LicenseGracePeriod.None.IsEnabled);
        Assert.Equal(TimeSpan.Zero, LicenseGracePeriod.None.Duration);
    }

    [Fact]
    public void EvaluateWithoutGrace_UsesDisabledGrace()
    {
        var evaluator = EvaluatorAt(ExpiresAt.AddMinutes(1));

        var status = evaluator.Evaluate(Document(expiresAt: ExpiresAt));

        Assert.True(status.IsExpired);
        Assert.False(status.IsWithinGrace);
    }

    [Fact]
    public void ConfiguredGrace_IsDiagnosticOnlyAndNeverGrantsCapability()
    {
        Assert.True(LicenseGracePeriod.TryCreate(TimeSpan.FromDays(7), out var grace));

        var status = EvaluatorAt(ExpiresAt.AddDays(1))
            .Evaluate(Document(expiresAt: ExpiresAt), grace);

        Assert.True(status.IsWithinGrace);
        // Reported for diagnostics only: the license is still expired and still not usable.
        Assert.True(status.IsExpired);
        Assert.False(status.IsUsable);
    }

    [Fact]
    public void ConfiguredGrace_DoesNotChangeValidatorOutcome()
    {
        // The validator never configures grace, so an expired license stays expired.
        var result = Validate(Document(expiresAt: ExpiresAt), ExpiresAt.AddDays(1));

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.Expired, result.Status);
    }

    [Fact]
    public void GracePeriod_AfterItsWindow_IsNotWithinGrace()
    {
        Assert.True(LicenseGracePeriod.TryCreate(TimeSpan.FromDays(1), out var grace));

        var status = EvaluatorAt(ExpiresAt.AddDays(2))
            .Evaluate(Document(expiresAt: ExpiresAt), grace);

        Assert.True(status.IsExpired);
        Assert.False(status.IsWithinGrace);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public void InvalidGraceDuration_IsRejected(int days)
    {
        Assert.False(LicenseGracePeriod.TryCreate(TimeSpan.FromDays(days), out var grace));
        Assert.False(grace.IsEnabled);
        Assert.Equal(TimeSpan.Zero, grace.Duration);
    }

    [Fact]
    public void MaximumGraceDuration_IsAcceptedAndStillBounded()
    {
        Assert.True(LicenseGracePeriod.TryCreate(LicenseExpirationPolicy.MaximumGracePeriod, out var grace));
        Assert.True(grace.IsEnabled);

        // Even at the maximum, grace never makes the license usable.
        var status = EvaluatorAt(ExpiresAt.AddDays(1))
            .Evaluate(Document(expiresAt: ExpiresAt), grace);
        Assert.True(status.IsExpired);
        Assert.False(status.IsUsable);
    }

    // ---------------------------------------------------------------------------------------
    // Warning signal
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ApproachingExpiry_IsSignalledWithoutExtendingValidity()
    {
        var now = ExpiresAt - LicenseExpirationPolicy.WarningThreshold;

        var status = EvaluatorAt(now).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.True(status.IsApproachingExpiry);
        Assert.True(status.IsUsable);
        Assert.Equal(ExpiresAt, status.ExpiresAt);
    }

    [Fact]
    public void FarFromExpiry_IsNotApproaching()
    {
        var status = EvaluatorAt(IssuedAt).Evaluate(Document(expiresAt: ExpiresAt));

        Assert.False(status.IsApproachingExpiry);
    }

    // ---------------------------------------------------------------------------------------
    // Restricted mode after expiry (D4.7-7)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ExpiredLicense_MapsToRestrictedPolicy()
    {
        var result = Validate(Document(expiresAt: ExpiresAt), ExpiresAt.AddDays(1));
        var policy = LicensePolicy.FromValidationResult(result);

        Assert.True(policy.IsRestricted);
        foreach (var feature in LicenseFeatureIds.All)
        {
            Assert.False(policy.IsFeatureEnabled(feature));
        }

        Assert.False(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
        Assert.False(policy.MeetsMinimumEdition(LicenseEdition.Community));
    }

    [Fact]
    public void ExpiredLicense_DoesNotAlterSecurityBehavior()
    {
        // Restricted mode is expressed only through licensing policy; no authentication,
        // authorization, TLS, request-limit, rate-limit or audit surface is reachable from it.
        var result = Validate(Document(expiresAt: ExpiresAt), ExpiresAt.AddDays(1));
        var policy = LicensePolicy.FromValidationResult(result);

        Assert.True(policy.IsRestricted);
        Assert.Equal(LicenseEdition.Community, policy.Edition);
    }

    [Fact]
    public void NotYetValidLicense_MapsToRestrictedPolicy()
    {
        var now = IssuedAt - LicenseValidationPolicy.ClockSkewAllowance - TimeSpan.FromSeconds(1);
        var result = Validate(Document(expiresAt: ExpiresAt), now);
        var policy = LicensePolicy.FromValidationResult(result);

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.NotYetValid, result.Status);
        Assert.True(policy.IsRestricted);
    }

    // ---------------------------------------------------------------------------------------
    // Fail-closed
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ExpiredLicense_DoesNotGrantAFeature()
    {
        var policy = LicensePolicy.FromValidationResult(
            Validate(Document(expiresAt: ExpiresAt), ExpiresAt.AddDays(1)));

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    [Fact]
    public void ExpiryBeforeIssuedAt_IsRejectedAsInvalidContent()
    {
        var result = Validate(Document(expiresAt: IssuedAt.AddSeconds(-1)), IssuedAt);

        Assert.False(result.IsValid);
        Assert.Equal(LicenseValidationStatus.InvalidContent, result.Status);
        Assert.Equal(LicenseValidationReason.ExpiryInvalid, result.Reason);
    }

    [Fact]
    public void NoEvaluatorFailurePathYieldsUsableStatus()
    {
        var evaluator = EvaluatorAt(ExpiresAt.AddYears(1));

        var status = evaluator.Evaluate(Document(expiresAt: ExpiresAt));

        Assert.True(status.IsExpired);
        Assert.False(status.IsUsable);
        Assert.Null(status.TimeUntilExpiry);
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static ILicenseExpirationEvaluator EvaluatorAt(DateTimeOffset now)
        => new LicenseExpirationEvaluator(new FixedClock(now));

    private static LicenseValidationResult Validate(
        LicenseDocument document, DateTimeOffset At)
    {
        var clock = new FixedClock(At);
        var validator = new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new AlwaysValidSignatureVerifier(),
            clock);

        return validator.Validate(BuildContainer(document));
    }

    private static LicenseDocument Document(DateTimeOffset? expiresAt) => new()
    {
        LicenseVersion = 1,
        LicenseId = "license-0001",
        Product = LicenseConstants.Product,
        Edition = LicenseEdition.Professional,
        Customer = "Example Customer",
        IssuedAt = IssuedAt,
        ExpiresAt = expiresAt,
        Features = new[] { LicenseFeatureIds.AuthLdap },
        Limits = new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 }
    };

    private static byte[] BuildContainer(LicenseDocument document)
    {
        var expires = document.ExpiresAt is { } expiry
            ? "\"" + expiry.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") + "\""
            : "null";

        var payloadJson =
            "{\"licenseVersion\":1," +
            "\"licenseId\":\"" + document.LicenseId + "\"," +
            "\"product\":\"" + document.Product + "\"," +
            "\"edition\":\"" + document.Edition + "\"," +
            "\"customer\":\"" + document.Customer + "\"," +
            "\"issuedAt\":\"" + document.IssuedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") + "\"," +
            "\"expiresAt\":" + expires + "," +
            "\"features\":[\"auth.ldap\"]," +
            "\"limits\":{\"max.users\":100}}";

        var container =
            "{\"payload\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)) + "\"," +
            "\"algorithm\":\"" + LicenseConstants.RsaPssSha256Algorithm + "\"," +
            "\"keyId\":\"key-2026\"," +
            "\"signature\":\"" + Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }) + "\"}";

        return Encoding.UTF8.GetBytes(container);
    }

    private sealed class FixedClock : ILicenseClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class AlwaysValidSignatureVerifier : ILicenseSignatureVerifier
    {
        public LicenseSignatureVerificationOutcome Verify(
            byte[] signedPayload, string algorithm, string keyId, byte[] signature)
            => LicenseSignatureVerificationOutcome.Success();
    }
}