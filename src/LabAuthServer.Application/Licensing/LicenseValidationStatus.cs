namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Coarse validation outcome. This is the public-safe category; it is intentionally
/// less detailed than <see cref="LicenseValidationReason"/>.
/// </summary>
public enum LicenseValidationStatus
{
    /// <summary>The license is valid for the current time.</summary>
    Valid = 0,

    /// <summary>No license was present. The application runs in Community/restricted mode.</summary>
    Missing = 1,

    /// <summary>The license could not be read or parsed.</summary>
    Malformed = 2,

    /// <summary>The license format version is not supported by this build.</summary>
    UnsupportedVersion = 3,

    /// <summary>The license was issued for a different product.</summary>
    WrongProduct = 4,

    /// <summary>The signature did not verify against a trusted key.</summary>
    InvalidSignature = 5,

    /// <summary>The signing key identifier is not in the trusted key set.</summary>
    UnknownKey = 6,

    /// <summary>The license has expired.</summary>
    Expired = 7,

    /// <summary>The license is not yet valid.</summary>
    NotYetValid = 8,

    /// <summary>The trusted key set or validation configuration is unusable.</summary>
    InvalidConfiguration = 9
}