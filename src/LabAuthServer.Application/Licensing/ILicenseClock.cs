namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Injectable time source for license validation (Phase 4.5 decision D4.5-6).
/// The clock is read through this abstraction so expiry and not-yet-valid boundaries are
/// deterministic in tests and never depend on a static ambient call.
/// </summary>
public interface ILicenseClock
{
    /// <summary>Current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>System clock implementation. Returns the current instant in UTC.</summary>
public sealed class SystemLicenseClock : ILicenseClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}