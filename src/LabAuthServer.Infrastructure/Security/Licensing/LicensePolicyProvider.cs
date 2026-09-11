using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Loads and validates the configured license file once at construction and exposes the
/// resulting policy (Phase 4.16). The lifecycle is deliberately simple and deterministic:
/// the license is loaded and validated when the provider is built (application startup),
/// and a replacement requires an application reload/restart. Any missing, unreadable,
/// oversized, malformed, untrusted, expired or otherwise invalid license yields
/// <see cref="LicensePolicy.Restricted"/>; the provider never throws for a licensing
/// condition and never weakens authentication or security controls. Only security and
/// operational metadata is logged; license content, payloads, signatures and key material
/// are never logged.
/// </summary>
public sealed class LicensePolicyProvider : ILicensePolicyProvider
{
    private readonly ILicensePolicy _policy;

    public LicensePolicyProvider(
        IOptions<LicenseValidationOptions> options,
        ILicenseFileReader fileReader,
        ILicenseValidator validator,
        ILogger<LicensePolicyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileReader);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        var settings = options.Value;
        var read = fileReader.Read(settings.LicenseFilePath, settings.MaximumLicenseFileBytes);

        if (!read.Succeeded)
        {
            logger.LogWarning(
                "License not loaded: {LicenseFileReadStatus}. Entering restricted Community mode.",
                read.Status);
            _policy = LicensePolicy.Restricted;
            return;
        }

        var result = validator.Validate(read.Content);
        if (!result.IsValid)
        {
            logger.LogWarning(
                "License validation failed: status {LicenseStatus}, reason {LicenseReason}. Entering restricted Community mode.",
                result.Status,
                result.Reason);
            _policy = LicensePolicy.Restricted;
            return;
        }

        _policy = LicensePolicy.FromValidationResult(result);
        logger.LogInformation(
            "License loaded and validated. Edition {LicenseEdition}, restricted {IsRestricted}.",
            _policy.Edition,
            _policy.IsRestricted);
    }

    /// <inheritdoc />
    public ILicensePolicy GetPolicy() => _policy;
}