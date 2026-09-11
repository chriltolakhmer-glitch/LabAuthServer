namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Configuration for license validation. The license file location is configurable
/// (Phase 4 decision D-13). No default path is hard-coded here; a missing configuration
/// yields restricted mode rather than an exception.
/// Phase 4.16 adds a bounded file-size limit and trusted public-key provisioning.
/// </summary>
public sealed class LicenseValidationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Licensing";

    /// <summary>Configured path to the license file. Empty means no license is configured.</summary>
    public string LicenseFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Largest license file accepted by the runtime loader. Bounds memory use so an
    /// oversized or hostile file can never be read unbounded (Phase 4.16).
    /// </summary>
    public long MaximumLicenseFileBytes { get; set; } = 64 * 1024;

    /// <summary>
    /// Trusted public verification keys. Provisioned by deployment configuration; the
    /// vendor private key is never present here or anywhere in the server (D-16).
    /// </summary>
    public List<TrustedLicenseKeyOptions> TrustedKeys { get; set; } = new();
}

/// <summary>
/// A single trusted public verification key. Only public material is ever supplied; the
/// key is copied from its public parameters and no private material is retained.
/// </summary>
public sealed class TrustedLicenseKeyOptions
{
    /// <summary>Stable key identifier matched against the license <c>keyId</c>.</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>PEM-encoded public key (SubjectPublicKeyInfo, <c>BEGIN PUBLIC KEY</c>).</summary>
    public string PublicKey { get; set; } = string.Empty;
}