namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Internal, stable reason codes used for diagnostics and tests.
/// These must never be surfaced through public API responses.
/// </summary>
public enum LicenseValidationReason
{
    /// <summary>No reason recorded (used only for successful validation).</summary>
    None = 0,

    /// <summary>No license file was found at the configured location.</summary>
    LicenseMissing = 1,

    /// <summary>The license file could not be read.</summary>
    LicenseUnreadable = 2,

    /// <summary>The license content could not be parsed as a well-formed document.</summary>
    LicenseMalformed = 3,

    /// <summary>The license format version is not supported.</summary>
    VersionUnsupported = 4,

    /// <summary>The product identifier did not match.</summary>
    ProductMismatch = 5,

    /// <summary>The signature envelope was missing or malformed.</summary>
    SignatureMalformed = 6,

    /// <summary>The signing key identifier is not trusted.</summary>
    KeyUntrusted = 7,

    /// <summary>The signing algorithm is not accepted.</summary>
    AlgorithmUnsupported = 8,

    /// <summary>The signature did not verify over the signed payload.</summary>
    SignatureInvalid = 9,

    /// <summary>A required field was missing.</summary>
    FieldMissing = 10,

    /// <summary>A field value was invalid (for example an unknown edition).</summary>
    FieldInvalid = 11,

    /// <summary>The license is not yet valid.</summary>
    NotYetValid = 12,

    /// <summary>The expiry instant was invalid or before the issuance instant.</summary>
    ExpiryInvalid = 13,

    /// <summary>The license has expired.</summary>
    Expired = 14,

    /// <summary>The trusted key set or validation configuration is unusable.</summary>
    InvalidConfiguration = 15,

    /// <summary>A property not defined by the license format was present (Phase 4.5, finding M-1).</summary>
    FieldUnknown = 16,

    /// <summary>A feature identifier is not in the known feature set (Phase 4.5).</summary>
    FeatureUnknown = 17,

    /// <summary>A limit value was outside the allowed range (Phase 4.5).</summary>
    LimitOutOfRange = 18
}