namespace LabAuthServer.Domain.Licensing;

/// <summary>Constant values shared by the license model, its parser and its consumers.</summary>
public static class LicenseConstants
{
    /// <summary>Product identifier every license must declare.</summary>
    public const string Product = "LabAuthServer";

    /// <summary>Oldest license format version this build understands.</summary>
    public const int MinimumSupportedLicenseVersion = 1;

    /// <summary>Newest license format version this build understands.</summary>
    public const int MaximumSupportedLicenseVersion = 1;

    /// <summary>
    /// Approved signing algorithm identifier for the initial implementation
    /// (RSA-PSS with SHA-256, RSA-3072). See Phase 4 decision D-11.
    /// </summary>
    public const string RsaPssSha256Algorithm = "RSA-PSS-SHA256";

    /// <summary>Minimum accepted RSA key size, aligned with the existing RSA key-size policy.</summary>
    public const int MinimumRsaKeySize = 2048;
}