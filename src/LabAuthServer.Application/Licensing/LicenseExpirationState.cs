namespace LabAuthServer.Application.Licensing;

/// <summary>Position of a license on the expiration timeline (Phase 4.7).</summary>
public enum LicenseExpirationState
{
    /// <summary>No usable expiry instant; the license never expires (D4.7-6).</summary>
    Perpetual = 0,

    /// <summary>Inside the licensed window; licensing capability may be granted.</summary>
    Active = 1,

    /// <summary>Before <c>issuedAt</c> beyond the documented clock-skew allowance.</summary>
    NotYetValid = 2,

    /// <summary>At or past <c>expiresAt</c>. Restricted mode; no grace is granted (D-08).</summary>
    Expired = 3
}