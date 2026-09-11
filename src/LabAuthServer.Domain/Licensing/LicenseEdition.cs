namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// Approved commercial editions (Phase 4 decision D-04).
/// An unrecognized edition must be treated as restricted/default-deny.
/// </summary>
public enum LicenseEdition
{
    /// <summary>Unrecognized or unspecified edition. Always default-deny.</summary>
    Unknown = 0,

    /// <summary>Community edition. Also the edition applied when no valid license is present.</summary>
    Community = 1,

    /// <summary>Professional edition.</summary>
    Professional = 2,

    /// <summary>Enterprise edition.</summary>
    Enterprise = 3
}