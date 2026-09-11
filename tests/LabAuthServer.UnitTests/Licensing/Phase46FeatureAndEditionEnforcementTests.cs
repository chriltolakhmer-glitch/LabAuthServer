using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.6 feature and edition enforcement tests. Verifies default-deny feature evaluation,
/// edition hierarchy behavior, explicit-feature entitlement, limit handling (including the
/// missing-limit and overflow boundaries), restricted-mode behavior and the security-independence
/// constraint. No private key, no network dependency.
/// </summary>
public sealed class Phase46FeatureAndEditionEnforcementTests
{
    // ---------------------------------------------------------------------------------------
    // Edition model
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData(LicenseEdition.Community, 1)]
    [InlineData(LicenseEdition.Professional, 2)]
    [InlineData(LicenseEdition.Enterprise, 3)]
    public void KnownEditions_HaveExpectedRank(LicenseEdition edition, int expectedRank)
    {
        Assert.True(edition.IsKnown());
        Assert.Equal(expectedRank, edition.Rank());
    }

    [Fact]
    public void UnknownEdition_HasNoRankAndIsNotKnown()
    {
        Assert.False(LicenseEdition.Unknown.IsKnown());
        Assert.Equal(-1, LicenseEdition.Unknown.Rank());
    }

    // ---------------------------------------------------------------------------------------
    // Features: allowed / denied / default-deny
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ExplicitlyLicensedFeature_IsAllowed()
    {
        var policy = PolicyFor(LicenseEdition.Professional, LicenseFeatureIds.AuthLdap);

        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
        Assert.True(policy.EvaluateFeature(LicenseFeatureIds.AuthLdap).IsAllowed);
    }

    [Fact]
    public void FeatureAbsentFromLicense_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise, LicenseFeatureIds.AuthLdap);

        var decision = policy.EvaluateFeature(LicenseFeatureIds.AdminConsole);

        Assert.False(decision.IsAllowed);
        Assert.Equal(LicenseValidationReason.FeatureUnknown, decision.Reason);
    }

    [Fact]
    public void UnknownFeatureIdentifier_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise, LicenseFeatureIds.AuthLdap);

        var decision = policy.EvaluateFeature("not.a.real.feature");

        Assert.False(decision.IsAllowed);
        Assert.Equal(LicenseValidationReason.FeatureUnknown, decision.Reason);
    }

    [Fact]
    public void UnknownFeatureCannotBeGrantedEvenWhenListedInLicense()
    {
        // A syntactically well-formed but catalog-unknown identifier is not a valid grant.
        var policy = PolicyFor(LicenseEdition.Enterprise, "future.unknown.feature");

        Assert.False(policy.IsFeatureEnabled("future.unknown.feature"));
    }

    [Fact]
    public void NullFeatureIdentifier_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise, LicenseFeatureIds.AuthLdap);

        var decision = policy.EvaluateFeature(null!);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void MultipleLicensedFeatures_AreEachEvaluatedIndependently()
    {
        var policy = PolicyFor(LicenseEdition.Professional, LicenseFeatureIds.AuthLdap, LicenseFeatureIds.AuditLogging);

        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuditLogging));
        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AdminConsole));
    }

    [Theory]
    [InlineData(LicenseEdition.Community, LicenseFeatureIds.AuthBasic, true)]
    [InlineData(LicenseEdition.Community, LicenseFeatureIds.AuthJwt, true)]
    [InlineData(LicenseEdition.Community, LicenseFeatureIds.AuthLdap, false)]
    [InlineData(LicenseEdition.Community, LicenseFeatureIds.AuditLogging, false)]
    [InlineData(LicenseEdition.Community, LicenseFeatureIds.AdminConsole, false)]
    [InlineData(LicenseEdition.Professional, LicenseFeatureIds.AuthBasic, true)]
    [InlineData(LicenseEdition.Professional, LicenseFeatureIds.AuthJwt, true)]
    [InlineData(LicenseEdition.Professional, LicenseFeatureIds.AuthLdap, true)]
    [InlineData(LicenseEdition.Professional, LicenseFeatureIds.AuditLogging, true)]
    [InlineData(LicenseEdition.Professional, LicenseFeatureIds.AdminConsole, false)]
    [InlineData(LicenseEdition.Enterprise, LicenseFeatureIds.AuthBasic, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseFeatureIds.AuthJwt, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseFeatureIds.AuthLdap, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseFeatureIds.AuditLogging, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseFeatureIds.AdminConsole, true)]
    public void ApprovedEditionMatrix_ControlsMaximumFeatureAvailability(
        LicenseEdition edition, string featureId, bool expected)
    {
        var policy = PolicyFor(edition, featureId);

        Assert.Equal(expected, policy.IsFeatureEnabled(featureId));
    }

    [Fact]
    public void CommunityEdition_CannotGrantProfessionalFeatureEvenWhenExplicitlyListed()
    {
        var policy = PolicyFor(LicenseEdition.Community, LicenseFeatureIds.AuthLdap);

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    [Fact]
    public void EmptyFeatureList_GrantsNothing()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise);

        foreach (var feature in LicenseFeatureIds.All)
        {
            Assert.False(policy.IsFeatureEnabled(feature));
        }
    }

    // ---------------------------------------------------------------------------------------
    // Edition + explicit feature combinations
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void CommunityEdition_WithCommunityFeature_IsAllowed()
    {
        var policy = PolicyFor(LicenseEdition.Community, LicenseFeatureIds.AuthBasic);

        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthBasic));
    }

    [Fact]
    public void CommunityEdition_WithoutProfessionalFeature_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Community, LicenseFeatureIds.AuthBasic);

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    [Fact]
    public void EditionAlone_DoesNotGrantFeature()
    {
        // Enterprise is the highest edition but features remain explicit (no implicit upgrade).
        var policy = PolicyFor(LicenseEdition.Enterprise);

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AdminConsole));
    }

    [Fact]
    public void ExplicitFeatureRestriction_IsRespectedEvenAtHighEdition()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise, LicenseFeatureIds.AuthJwt);

        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthJwt));
        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    [Theory]
    [InlineData(LicenseEdition.Community, LicenseEdition.Community, true)]
    [InlineData(LicenseEdition.Professional, LicenseEdition.Community, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseEdition.Community, true)]
    [InlineData(LicenseEdition.Professional, LicenseEdition.Professional, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseEdition.Professional, true)]
    [InlineData(LicenseEdition.Enterprise, LicenseEdition.Enterprise, true)]
    [InlineData(LicenseEdition.Community, LicenseEdition.Professional, false)]
    [InlineData(LicenseEdition.Professional, LicenseEdition.Enterprise, false)]
    public void MeetsMinimumEdition_FollowsHierarchy(
        LicenseEdition edition, LicenseEdition minimum, bool expected)
    {
        var policy = PolicyFor(edition);

        Assert.Equal(expected, policy.MeetsMinimumEdition(minimum));
    }

    [Fact]
    public void MeetsMinimumEdition_UnknownMinimum_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise);

        Assert.False(policy.MeetsMinimumEdition(LicenseEdition.Unknown));
    }

    // ---------------------------------------------------------------------------------------
    // Restricted mode (missing / invalid license)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void RestrictedPolicy_DeniesEveryFeature()
    {
        var policy = LicensePolicy.Restricted;

        foreach (var feature in LicenseFeatureIds.All)
        {
            Assert.False(policy.IsFeatureEnabled(feature));
        }
    }

    [Fact]
    public void RestrictedPolicy_ReportsCommunityEdition()
    {
        Assert.Equal(LicenseEdition.Community, LicensePolicy.Restricted.Edition);
        Assert.True(LicensePolicy.Restricted.IsRestricted);
    }

    [Fact]
    public void RestrictedPolicy_DefinesNoLimits()
    {
        Assert.False(LicensePolicy.Restricted.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
    }

    [Fact]
    public void RestrictedPolicy_FailsMinimumEdition()
    {
        Assert.False(LicensePolicy.Restricted.MeetsMinimumEdition(LicenseEdition.Community));
    }

    [Theory]
    [InlineData(LicenseValidationStatus.Missing)]
    [InlineData(LicenseValidationStatus.Malformed)]
    [InlineData(LicenseValidationStatus.UnsupportedVersion)]
    [InlineData(LicenseValidationStatus.WrongProduct)]
    [InlineData(LicenseValidationStatus.InvalidSignature)]
    [InlineData(LicenseValidationStatus.UnknownKey)]
    [InlineData(LicenseValidationStatus.Expired)]
    [InlineData(LicenseValidationStatus.NotYetValid)]
    [InlineData(LicenseValidationStatus.InvalidConfiguration)]
    [InlineData(LicenseValidationStatus.InvalidContent)]
    public void InvalidValidationResult_YieldsRestrictedPolicy(LicenseValidationStatus status)
    {
        var result = LicenseValidationResult.Failed(status, LicenseValidationReason.FieldInvalid);

        var policy = LicensePolicy.FromValidationResult(result);

        Assert.True(policy.IsRestricted);
        foreach (var feature in LicenseFeatureIds.All)
        {
            Assert.False(policy.IsFeatureEnabled(feature));
        }
    }

    [Fact]
    public void ValidValidationResult_YieldsEntitledPolicy()
    {
        var document = Document(LicenseEdition.Professional, LicenseFeatureIds.AuthLdap);
        var result = LicenseValidationResult.Valid(document);

        var policy = LicensePolicy.FromValidationResult(result);

        Assert.False(policy.IsRestricted);
        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    // ---------------------------------------------------------------------------------------
    // Limits
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void MaximumUsersLimit_UnderLimit_IsWithin()
    {
        var policy = PolicyFor(LicenseEdition.Professional,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 });

        Assert.True(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out var limit));
        Assert.Equal(100, limit);
        Assert.True(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 50));
    }

    [Fact]
    public void MaximumUsersLimit_ExactlyAtLimit_IsWithin()
    {
        var policy = PolicyFor(LicenseEdition.Professional,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 });

        Assert.True(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 100));
    }

    [Fact]
    public void MaximumUsersLimit_AboveLimit_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Professional,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 });

        Assert.False(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 101));
    }

    [Fact]
    public void MissingLimit_IsNeverUnlimited()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise);

        Assert.False(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
        Assert.False(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 0));
        Assert.False(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 1));
    }

    [Fact]
    public void UnknownLimitKey_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 });

        Assert.False(policy.TryGetLimit("max.something", out _));
        Assert.False(policy.IsWithinLimit("max.something", 1));
    }

    [Fact]
    public void MaximumIntegerLimit_DoesNotOverflow()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = int.MaxValue });

        Assert.True(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, int.MaxValue));
        Assert.True(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, int.MaxValue - 1));
    }

    [Fact]
    public void NegativeUsage_IsDenied()
    {
        var policy = PolicyFor(LicenseEdition.Enterprise,
            limits: new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 });

        Assert.False(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, -1));
    }

    // ---------------------------------------------------------------------------------------
    // Security independence (decision D4.6-6)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void SecurityControls_AreNotExpressedThroughThePolicy()
    {
        // The policy exposes only commercial capability and limits; there is no API by which
        // enabling or disabling licensing could alter authentication, authorization, TLS,
        // request limits, rate limiting, audit or JWT behavior.
        var policy = LicensePolicy.Restricted;

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthBasic));
        // Restricted mode still reports an edition value; it does not instruct the request
        // pipeline to change any security control.
        Assert.Equal(LicenseEdition.Community, policy.Edition);
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static LicensePolicy PolicyFor(
        LicenseEdition edition,
        params string[] features)
        => PolicyFor(edition, features, limits: null);

    private static LicensePolicy PolicyFor(
        LicenseEdition edition,
        string[] features,
        Dictionary<string, int>? limits)
        => LicensePolicy.FromDocument(new LicenseDocument
        {
            LicenseVersion = 1,
            LicenseId = "license-test",
            Product = LicenseConstants.Product,
            Edition = edition,
            IssuedAt = new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
            Features = features,
            Limits = limits ?? new Dictionary<string, int>()
        });

    private static LicensePolicy PolicyFor(
        LicenseEdition edition,
        Dictionary<string, int> limits)
        => PolicyFor(edition, Array.Empty<string>(), limits);

    private static LicensePolicy PolicyFor(
        LicenseEdition edition,
        string feature,
        Dictionary<string, int> limits)
        => PolicyFor(edition, new[] { feature }, limits);

    private static LicenseDocument Document(LicenseEdition edition, params string[] features) => new()
    {
        LicenseVersion = 1,
        LicenseId = "license-test",
        Product = LicenseConstants.Product,
        Edition = edition,
        IssuedAt = new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
        Features = features,
        Limits = new Dictionary<string, int>()
    };
}