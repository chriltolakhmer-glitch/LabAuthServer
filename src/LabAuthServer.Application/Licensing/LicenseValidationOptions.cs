namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Configuration for license validation. The license file location is configurable
/// (Phase 4 decision D-13). No default path is hard-coded here; a missing configuration
/// yields restricted mode rather than an exception.
/// </summary>
public sealed class LicenseValidationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Licensing";

    /// <summary>Configured path to the license file. Empty means no license is configured.</summary>
    public string LicenseFilePath { get; set; } = string.Empty;
}