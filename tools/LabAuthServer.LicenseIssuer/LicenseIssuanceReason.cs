namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Internal, stable issuance failure reasons. These must never be surfaced to a customer or
/// through a public API. <see cref="None"/> means issuance succeeded.
/// </summary>
public enum LicenseIssuanceReason
{
    /// <summary>Issuance succeeded.</summary>
    None = 0,

    /// <summary>The license identifier was missing or whitespace.</summary>
    LicenseIdMissing = 1,

    /// <summary>The product identifier did not match the expected product.</summary>
    ProductInvalid = 2,

    /// <summary>The requested edition is not a known edition.</summary>
    EditionUnknown = 3,

    /// <summary>The issuance instant was not a usable timestamp.</summary>
    IssuedAtInvalid = 4,

    /// <summary>The expiry instant was present but not a usable timestamp.</summary>
    ExpiresAtInvalid = 5,

    /// <summary>The expiry instant was not after the issuance instant.</summary>
    ExpiresAtBeforeIssuedAt = 6,

    /// <summary>The signing key identifier was missing or whitespace.</summary>
    KeyIdMissing = 7,

    /// <summary>The requested signing key identifier did not match the configured signing key.</summary>
    KeyIdMismatch = 8,

    /// <summary>No usable signing key was available, or the key could not sign.</summary>
    SigningKeyUnavailable = 9,

    /// <summary>The signing key did not satisfy the RSA key-size policy.</summary>
    SigningKeyPolicyViolation = 10,

    /// <summary>A feature identifier was structurally invalid.</summary>
    FeatureInvalid = 11,

    /// <summary>A limit identifier or value was invalid.</summary>
    LimitInvalid = 12,

    /// <summary>The canonical payload could not be serialized.</summary>
    PayloadSerializationFailed = 13
}