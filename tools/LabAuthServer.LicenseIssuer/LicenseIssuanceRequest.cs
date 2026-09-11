using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Structured input for a single license issuance. Timestamps are UTC; <see cref="ExpiresAt"/>
/// is <c>null</c> for a perpetual license. Every value is validated before any signing occurs.
/// </summary>
public sealed class LicenseIssuanceRequest
{
    /// <summary>Unique, opaque license identifier. Required; not generated in this phase.</summary>
    public required string LicenseId { get; init; }

    /// <summary>Product identifier. Must equal <see cref="LicenseConstants.Product"/>.</summary>
    public string Product { get; init; } = LicenseConstants.Product;

    /// <summary>Edition granted by the license. Must be a known edition.</summary>
    public required LicenseEdition Edition { get; init; }

    /// <summary>Optional licensee display name. Treated as sensitive; never publicly exposed.</summary>
    public string? Customer { get; init; }

    /// <summary>UTC issuance instant.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>UTC expiry instant, or <c>null</c> for a perpetual license.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Explicit feature identifiers. Structural validation only in this phase.</summary>
    public IReadOnlyList<string> Features { get; init; } = Array.Empty<string>();

    /// <summary>License limits keyed by identifier. Values must be positive integers.</summary>
    public IReadOnlyDictionary<string, int> Limits { get; init; } = new Dictionary<string, int>();

    /// <summary>Signing key identifier. Must match the configured signing key provider.</summary>
    public required string KeyId { get; init; }
}