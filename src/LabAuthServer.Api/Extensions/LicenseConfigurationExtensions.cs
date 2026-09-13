using LabAuthServer.Application.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;

namespace LabAuthServer.Api.Extensions;

/// <summary>
/// Wires the offline license subsystem (Phase 4.16): the bounded file reader, the
/// public-only trusted key set, the strict parser, the RSA-PSS verifier, the validator and
/// the policy provider that loads and validates the license at startup and re-evaluates expiry
/// during policy access. Registration
/// fails closed: with no configured keys or license the policy is restricted Community.
/// No private key is required or read.
/// </summary>
public static class LicenseConfigurationExtensions
{
    public static IServiceCollection AddLicenseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<LicenseValidationOptions>()
            .Bind(configuration.GetSection(LicenseValidationOptions.SectionName));

        services.AddSingleton<ILicenseClock, SystemLicenseClock>();
        services.AddSingleton<ILicenseFileReader, BoundedLicenseFileReader>();

        services.AddSingleton<ILicenseDocumentParser, JsonLicenseDocumentParser>();
        services.AddSingleton<ILicenseSignatureVerifier>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<LicenseValidationOptions>>()
                .Value;

            var provider = new InMemoryTrustedLicenseKeyProvider();
            if (!TrustedKeySetFactory.Populate(provider, options.TrustedKeys))
            {
                serviceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("LabAuthServer.Licensing")
                    .LogWarning("Trusted license key configuration is invalid; entering restricted Community mode.");
                provider.Dispose();
                provider = new InMemoryTrustedLicenseKeyProvider();
            }

            return new RsaPssLicenseSignatureVerifier(provider);
        });

        services.AddSingleton<ILicenseExpirationEvaluator, LicenseExpirationEvaluator>();
        services.AddSingleton<ILicenseValidator, LicenseValidator>();
        services.AddSingleton<ILicensePolicyProvider, LicensePolicyProvider>();

        return services;
    }
}