namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Typed expiration outcome for a validated license (Phase 4.7). Produced by
/// <see cref="ILicenseExpirationEvaluator"/>. It never mutates the license document and never
/// extends validity: a grace period, when explicitly configured, is reported diagnostically and
/// does not make an expired license usable (decision D-08).
/// </summary>
public sealed class LicenseExpirationStatus
{
    private LicenseExpirationStatus(
        LicenseExpirationState state,
        DateTimeOffset issuedAt,
        DateTimeOffset? expiresAt,
        TimeSpan? timeUntilExpiry,
        bool isWithinGrace)
    {
        State = state;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        TimeUntilExpiry = timeUntilExpiry;
        IsWithinGrace = isWithinGrace;
    }

    /// <summary>Position on the expiration timeline.</summary>
    public LicenseExpirationState State { get; }

    /// <summary>UTC issuance instant taken from the license document.</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>UTC expiry instant, or <c>null</c> for a perpetual license.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>Remaining licensed time. <c>null</c> for perpetual, not-yet-valid or expired licenses.</summary>
    public TimeSpan? TimeUntilExpiry { get; }

    /// <summary>
    /// True only when the observed time falls inside an explicitly configured grace window.
    /// This is a diagnostic signal; it never grants licensing capability in this phase.
    /// </summary>
    public bool IsWithinGrace { get; }

    /// <summary>True when the license has no expiry instant.</summary>
    public bool IsPerpetual => State == LicenseExpirationState.Perpetual;

    /// <summary>True when the license is at or past <c>expiresAt</c>.</summary>
    public bool IsExpired => State == LicenseExpirationState.Expired;

    /// <summary>True when the license is before <c>issuedAt</c> beyond the skew allowance.</summary>
    public bool IsNotYetValid => State == LicenseExpirationState.NotYetValid;

    /// <summary>True only when the license may grant licensing capability (perpetual or active).</summary>
    public bool IsUsable => State is LicenseExpirationState.Perpetual or LicenseExpirationState.Active;

    /// <summary>
    /// True when an active license is inside the documented warning threshold. An observable
    /// operator signal only; it never extends validity.
    /// </summary>
    public bool IsApproachingExpiry
        => State == LicenseExpirationState.Active
           && TimeUntilExpiry is { } remaining
           && remaining <= LicenseExpirationPolicy.WarningThreshold;

    internal static LicenseExpirationStatus Perpetual(DateTimeOffset issuedAt)
        => new(LicenseExpirationState.Perpetual, issuedAt, null, null, isWithinGrace: false);

    internal static LicenseExpirationStatus Active(
        DateTimeOffset issuedAt, DateTimeOffset expiresAt, TimeSpan remaining)
        => new(LicenseExpirationState.Active, issuedAt, expiresAt, remaining, isWithinGrace: false);

    internal static LicenseExpirationStatus NotYetValid(DateTimeOffset issuedAt, DateTimeOffset? expiresAt)
        => new(LicenseExpirationState.NotYetValid, issuedAt, expiresAt, null, isWithinGrace: false);

    internal static LicenseExpirationStatus Expired(
        DateTimeOffset issuedAt, DateTimeOffset expiresAt, bool isWithinGrace)
        => new(LicenseExpirationState.Expired, issuedAt, expiresAt, null, isWithinGrace);
}