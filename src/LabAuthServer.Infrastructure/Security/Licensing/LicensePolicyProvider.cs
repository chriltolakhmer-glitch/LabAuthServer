using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Loads and validates the configured license file at construction, then re-evaluates the
/// validated document's expiration on every policy access. Any missing, unreadable, oversized,
/// malformed, untrusted, expired or otherwise invalid license yields
/// <see cref="LicensePolicy.Restricted"/>; the provider never throws for a licensing condition
/// and never weakens authentication or security controls. Only security and operational metadata
/// is logged; license content, payloads, signatures and key material are never logged.
/// </summary>
public sealed class LicensePolicyProvider : ILicensePolicyProvider
{
    private readonly ILicensePolicy _initialPolicy;
    private readonly LicenseDocument? _validatedDocument;
    private readonly ILicenseExpirationEvaluator _expirationEvaluator;
    private readonly ILogger<LicensePolicyProvider> _logger;

    public LicensePolicyProvider(
        IOptions<LicenseValidationOptions> options,
        ILicenseFileReader fileReader,
        ILicenseValidator validator,
        ILogger<LicensePolicyProvider> logger,
        ILicenseExpirationEvaluator? expirationEvaluator = null,
        ILicenseClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileReader);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _expirationEvaluator = expirationEvaluator ?? new LicenseExpirationEvaluator(clock ?? new SystemLicenseClock());
        _validatedDocument = null;

        try
        {
            var settings = options.Value;
            var read = fileReader.Read(settings.LicenseFilePath, settings.MaximumLicenseFileBytes);

            if (!read.Succeeded)
            {
                logger.LogWarning(
                    "License not loaded: {LicenseFileReadStatus}. Entering restricted Community mode.",
                    read.Status);
                _initialPolicy = LicensePolicy.Restricted;
                return;
            }

            var result = validator.Validate(read.Content);
            if (!result.IsValid || result.Policy is null)
            {
                logger.LogWarning(
                    "License validation failed: status {LicenseStatus}, reason {LicenseReason}. Entering restricted Community mode.",
                    result.Status,
                    result.Reason);
                _initialPolicy = LicensePolicy.Restricted;
                return;
            }

            _validatedDocument = result.Policy;
            _initialPolicy = LicensePolicy.FromDocument(result.Policy);
            logger.LogInformation(
                "License loaded and validated. Edition {LicenseEdition}, restricted {IsRestricted}.",
                _initialPolicy.Edition,
                _initialPolicy.IsRestricted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "License initialization failed. Entering restricted Community mode.");
            _initialPolicy = LicensePolicy.Restricted;
        }
    }

    /// <inheritdoc />
    public ILicensePolicy GetPolicy()
    {
        if (_validatedDocument is null)
        {
            return _initialPolicy;
        }

        var expiration = _expirationEvaluator.Evaluate(_validatedDocument);
        if (expiration.IsExpired || expiration.IsNotYetValid)
        {
            _logger.LogWarning(
                "License is no longer active at runtime: {LicenseExpirationState}. Entering restricted Community mode.",
                expiration.State);
            return LicensePolicy.Restricted;
        }

        return _initialPolicy;
    }
}