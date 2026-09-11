using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Immutable licensing policy built from an already-validated license (Phase 4.6).
/// Features are default-deny: a feature is enabled only when it is a known identifier and the
/// license explicitly lists it. Edition alone never grants a feature, and the commercial
/// feature-to-edition mapping remains TO BE CONFIRMED DURING IMPLEMENTATION (O-01).
/// </summary>
public sealed class LicensePolicy : ILicensePolicy
{
    private readonly IReadOnlySet<string> _grantedFeatures;
    private readonly IReadOnlyDictionary<string, int> _limits;

    private LicensePolicy(
        LicenseEdition edition,
        bool isRestricted,
        IReadOnlySet<string> grantedFeatures,
        IReadOnlyDictionary<string, int> limits)
    {
        Edition = edition;
        IsRestricted = isRestricted;
        _grantedFeatures = grantedFeatures;
        _limits = limits;
    }

    /// <inheritdoc />
    public LicenseEdition Edition { get; }

    /// <inheritdoc />
    public bool IsRestricted { get; }

    /// <summary>
    /// Community/restricted policy used when no valid license is in effect
    /// (Phase 4 decisions D-03, D-14). No optional feature is granted and no limit is defined.
    /// </summary>
    public static LicensePolicy Restricted { get; } = new(
        LicenseEdition.Community,
        isRestricted: true,
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, int>(StringComparer.Ordinal));

    /// <summary>Builds a policy from a validated license document.</summary>
    public static LicensePolicy FromDocument(LicenseDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var features = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in document.Features)
        {
            features.Add(feature);
        }

        var limits = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var limit in document.Limits)
        {
            limits[limit.Key] = limit.Value;
        }

        return new LicensePolicy(document.Edition, isRestricted: false, features, limits);
    }

    /// <summary>
    /// Builds a policy from a validation result. Any non-valid result yields the restricted
    /// policy; a validation failure never grants a capability.
    /// </summary>
    public static LicensePolicy FromValidationResult(LicenseValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsValid && result.Policy is not null
            ? FromDocument(result.Policy)
            : Restricted;
    }

    /// <inheritdoc />
    public LicenseFeatureDecision EvaluateFeature(string featureId)
    {
        if (featureId is null)
        {
            return LicenseFeatureDecision.Denied(string.Empty, LicenseValidationReason.FeatureUnknown);
        }

        if (IsRestricted)
        {
            return LicenseFeatureDecision.Denied(featureId, LicenseValidationReason.LicenseMissing);
        }

        if (!LicenseFeatureIds.IsKnown(featureId))
        {
            return LicenseFeatureDecision.Denied(featureId, LicenseValidationReason.FeatureUnknown);
        }

        return _grantedFeatures.Contains(featureId)
            ? LicenseFeatureDecision.Allowed(featureId)
            : LicenseFeatureDecision.Denied(featureId, LicenseValidationReason.FeatureUnknown);
    }

    /// <inheritdoc />
    public bool IsFeatureEnabled(string featureId) => EvaluateFeature(featureId).IsAllowed;

    /// <inheritdoc />
    public bool TryGetLimit(string limitKey, out int value)
    {
        value = 0;

        if (IsRestricted || limitKey is null || !LicenseLimitKeys.IsKnown(limitKey))
        {
            return false;
        }

        return _limits.TryGetValue(limitKey, out value);
    }

    /// <inheritdoc />
    public bool IsWithinLimit(string limitKey, int currentUsage)
    {
        if (currentUsage < 0)
        {
            return false;
        }

        return TryGetLimit(limitKey, out var limit) && currentUsage <= limit;
    }

    /// <inheritdoc />
    public bool MeetsMinimumEdition(LicenseEdition minimumEdition)
    {
        if (!minimumEdition.IsKnown() || IsRestricted)
        {
            return false;
        }

        return Edition.Rank() >= minimumEdition.Rank();
    }
}