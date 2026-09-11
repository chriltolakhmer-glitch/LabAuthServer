using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Typed, structured validation outcome. A boolean-only contract is deliberately avoided
/// (Phase 4 decision D-21) so callers can distinguish causes safely.
/// </summary>
public sealed class LicenseValidationResult
{
    private LicenseValidationResult(
        LicenseValidationStatus status,
        LicenseValidationReason reason,
        LicenseDocument? policy)
    {
        Status = status;
        Reason = reason;
        Policy = policy;
    }

    /// <summary>Public-safe status category.</summary>
    public LicenseValidationStatus Status { get; }

    /// <summary>Internal diagnostic reason code. Never expose through public API responses.</summary>
    public LicenseValidationReason Reason { get; }

    /// <summary>The effective license when validation succeeded; otherwise <c>null</c>.</summary>
    public LicenseDocument? Policy { get; }

    /// <summary>True only when the license is valid.</summary>
    public bool IsValid => Status == LicenseValidationStatus.Valid;

    /// <summary>Builds a successful result carrying the validated license.</summary>
    public static LicenseValidationResult Valid(LicenseDocument policy)
        => new(LicenseValidationStatus.Valid, LicenseValidationReason.None,
            policy ?? throw new ArgumentNullException(nameof(policy)));

    /// <summary>Builds a failure result with no policy.</summary>
    public static LicenseValidationResult Failed(LicenseValidationStatus status, LicenseValidationReason reason)
    {
        if (status == LicenseValidationStatus.Valid)
        {
            throw new ArgumentException("A failure result cannot have status Valid.", nameof(status));
        }

        return new(status, reason, null);
    }
}