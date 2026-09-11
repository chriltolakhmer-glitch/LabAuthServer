using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Evaluates the expiration timeline of an already-validated license (Phase 4.7).
/// This is the single place the licensed time window is decided. It reuses
/// <see cref="ILicenseClock"/> and never re-implements the structural or cryptographic validation
/// performed by Phase 4.5.
/// </summary>
public interface ILicenseExpirationEvaluator
{
    /// <summary>Evaluates <paramref name="document"/> using the supplied grace configuration.</summary>
    LicenseExpirationStatus Evaluate(LicenseDocument document, LicenseGracePeriod gracePeriod);

    /// <summary>
    /// Evaluates <paramref name="document"/> with no grace period, which is the approved default
    /// (decision D-08).
    /// </summary>
    LicenseExpirationStatus Evaluate(LicenseDocument document);
}

/// <summary>Default <see cref="ILicenseExpirationEvaluator"/> over an injectable UTC clock.</summary>
public sealed class LicenseExpirationEvaluator : ILicenseExpirationEvaluator
{
    private readonly ILicenseClock _clock;

    public LicenseExpirationEvaluator(ILicenseClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public LicenseExpirationStatus Evaluate(LicenseDocument document)
        => Evaluate(document, LicenseGracePeriod.None);

    /// <inheritdoc />
    public LicenseExpirationStatus Evaluate(LicenseDocument document, LicenseGracePeriod gracePeriod)
    {
        ArgumentNullException.ThrowIfNull(document);

        var now = _clock.UtcNow;
        var issuedAt = document.IssuedAt;

        // Not-yet-valid: issuedAt is in the future beyond the documented clock-skew allowance.
        if (issuedAt - LicenseValidationPolicy.ClockSkewAllowance > now)
        {
            return LicenseExpirationStatus.NotYetValid(issuedAt, document.ExpiresAt);
        }

        // Perpetual: an absent expiry instant never expires and never enters a grace state.
        if (document.ExpiresAt is not { } expiresAt)
        {
            return LicenseExpirationStatus.Perpetual(issuedAt);
        }

        if (now < expiresAt)
        {
            return LicenseExpirationStatus.Active(issuedAt, expiresAt, expiresAt - now);
        }

        // At or past expiry. Grace, when explicitly configured, is reported for diagnostics only:
        // it never grants licensing capability (D-08, no grace period approved).
        var withinGrace = gracePeriod.IsEnabled && now < expiresAt + gracePeriod.Duration;
        return LicenseExpirationStatus.Expired(issuedAt, expiresAt, withinGrace);
    }
}