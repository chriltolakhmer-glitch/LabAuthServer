namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// Immutable, strongly typed representation of a license payload.
/// The signed bytes are carried separately by <see cref="SignedLicense"/> so that
/// ordinary property serialization is never an implicit cryptographic contract.
/// </summary>
public sealed record LicenseDocument
{
    /// <summary>License format version. Values outside the supported range are rejected at parse time.</summary>
    public required int LicenseVersion { get; init; }

    /// <summary>Unique, opaque, vendor-generated license identifier.</summary>
    public required string LicenseId { get; init; }

    /// <summary>Product identifier. Must equal <see cref="LicenseConstants.Product"/>.</summary>
    public required string Product { get; init; }

    /// <summary>Edition granted by the license.</summary>
    public required LicenseEdition Edition { get; init; }

    /// <summary>Optional licensee display name. Treated as sensitive; never surfaced through public API responses.</summary>
    public string? Customer { get; init; }

    /// <summary>UTC issuance instant.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>UTC expiry instant, or <c>null</c> for a perpetual license.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Explicit feature identifiers granted by the license. Unknown identifiers are default-deny at validation.</summary>
    public IReadOnlyList<string> Features { get; init; } = Array.Empty<string>();

    /// <summary>License limits keyed by a known limit identifier, for example a maximum-user count.</summary>
    public IReadOnlyDictionary<string, int> Limits { get; init; } = new Dictionary<string, int>();

    /// <summary>True when the license has no expiry instant (perpetual license).</summary>
    public bool IsPerpetual => ExpiresAt is null;
}