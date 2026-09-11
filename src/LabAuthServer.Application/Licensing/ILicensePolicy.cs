using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Application.Licensing;

/// <summary>
/// The effective licensing policy consumed by licensed feature code (Phase 4.6, decision D4.6-5).
/// A single object is the decision source; security-relevant code never consults it
/// (decision D4.6-6). A policy read failure must deny, never allow.
/// </summary>
public interface ILicensePolicy
{
    /// <summary>The edition granted by the effective license, or Community in restricted mode.</summary>
    LicenseEdition Edition { get; }

    /// <summary>True when no valid license is in effect (Community/restricted mode, decision D-03).</summary>
    bool IsRestricted { get; }

    /// <summary>Evaluates a licensed feature. Unknown and absent features are denied.</summary>
    LicenseFeatureDecision EvaluateFeature(string featureId);

    /// <summary>True when the effective license explicitly grants <paramref name="featureId"/>.</summary>
    bool IsFeatureEnabled(string featureId);

    /// <summary>
    /// Returns the licensed limit for <paramref name="limitKey"/>. Returns <c>false</c> when the
    /// key is unknown or the license does not define it; a missing limit is never unlimited.
    /// </summary>
    bool TryGetLimit(string limitKey, out int value);

    /// <summary>
    /// True only when the license defines <paramref name="limitKey"/> and
    /// <paramref name="currentUsage"/> does not exceed it. A missing limit denies.
    /// </summary>
    bool IsWithinLimit(string limitKey, int currentUsage);

    /// <summary>True when the effective edition is at least <paramref name="minimumEdition"/>.</summary>
    bool MeetsMinimumEdition(LicenseEdition minimumEdition);
}