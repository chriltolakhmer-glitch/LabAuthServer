namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Expiration and grace bounds for the current Phase 4.7 implementation.
/// No grace period is approved (Phase 4 decision D-08) and no warning threshold is an approved
/// commercial decision; both values are implementation defaults and remain TO BE CONFIRMED
/// DURING IMPLEMENTATION (O-11, O-13). They are bounded so a misconfiguration can never become
/// a hidden permanent grace period.
/// </summary>
public static class LicenseExpirationPolicy
{
    /// <summary>
    /// Largest accepted grace duration. Bounds an accidental or hostile configuration so grace
    /// can never be effectively permanent.
    /// </summary>
    public static readonly TimeSpan MaximumGracePeriod = TimeSpan.FromDays(90);

    /// <summary>
    /// Operator warning lead time before <c>expiresAt</c>. A warning is an observable signal
    /// only: it never extends validity and never grants licensing capability.
    /// </summary>
    public static readonly TimeSpan WarningThreshold = TimeSpan.FromDays(30);
}