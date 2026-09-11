namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Typed outcome of a licensed-feature check (Phase 4.6, decisions D4.6-2, D4.6-3, D4.6-7).
/// Denials fail closed and carry only an internal reason code; no licensing detail is exposed
/// to a caller.
/// </summary>
public sealed class LicenseFeatureDecision
{
    private LicenseFeatureDecision(string featureId, bool allowed, LicenseValidationReason reason)
    {
        FeatureId = featureId;
        IsAllowed = allowed;
        Reason = reason;
    }

    /// <summary>The feature identifier that was evaluated.</summary>
    public string FeatureId { get; }

    /// <summary>True only when the feature is licensed and the license is valid.</summary>
    public bool IsAllowed { get; }

    /// <summary>Internal diagnostic reason. Never surface through a public API response.</summary>
    public LicenseValidationReason Reason { get; }

    /// <summary>Builds an allow decision.</summary>
    public static LicenseFeatureDecision Allowed(string featureId)
        => new(
            featureId ?? throw new ArgumentNullException(nameof(featureId)),
            allowed: true,
            LicenseValidationReason.None);

    /// <summary>Builds a deny decision with an internal reason code.</summary>
    public static LicenseFeatureDecision Denied(string featureId, LicenseValidationReason reason)
    {
        if (reason == LicenseValidationReason.None)
        {
            throw new ArgumentException("A denial requires a non-None reason.", nameof(reason));
        }

        return new(featureId ?? string.Empty, allowed: false, reason);
    }
}