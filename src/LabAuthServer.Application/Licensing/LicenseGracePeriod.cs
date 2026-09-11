namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Explicit, bounded, default-disabled grace period (Phase 4.7, decision D-08).
/// No grace period is approved for the initial implementation, so <see cref="None"/> is the only
/// value any licensing path uses. The type exists so that a future approved decision can be
/// represented safely: creation is validated, the duration is bounded, and an enabled grace
/// period never grants licensing capability in this phase.
/// </summary>
public readonly struct LicenseGracePeriod : IEquatable<LicenseGracePeriod>
{
    private LicenseGracePeriod(TimeSpan duration)
    {
        Duration = duration;
    }

    /// <summary>Disabled grace period. This is the approved initial configuration (D-08).</summary>
    public static LicenseGracePeriod None { get; } = new(TimeSpan.Zero);

    /// <summary>Configured grace duration. <see cref="TimeSpan.Zero"/> means disabled.</summary>
    public TimeSpan Duration { get; }

    /// <summary>True only when a positive, bounded grace duration is configured.</summary>
    public bool IsEnabled => Duration > TimeSpan.Zero;

    /// <summary>
    /// Validates a candidate duration. Negative values and values above
    /// <see cref="LicenseExpirationPolicy.MaximumGracePeriod"/> are rejected so an invalid
    /// configuration can never become an unlimited grace period.
    /// </summary>
    public static bool TryCreate(TimeSpan duration, out LicenseGracePeriod gracePeriod)
    {
        gracePeriod = None;

        if (duration < TimeSpan.Zero || duration > LicenseExpirationPolicy.MaximumGracePeriod)
        {
            return false;
        }

        gracePeriod = new LicenseGracePeriod(duration);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(LicenseGracePeriod other) => Duration == other.Duration;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LicenseGracePeriod other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Duration.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Duration.ToString();
}