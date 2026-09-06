using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;

namespace LabAuthServer.Api.Extensions;

public static class ActiveDirectoryOptionsExtensions
{
    public static IServiceCollection AddActiveDirectoryOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services
            .AddOptions<LdapOptions>()
            .Bind(configuration.GetSection(LdapOptions.SectionName))
            .Validate(
                options => LdapOptionsValidator.Validate(options, environment.IsProduction()).Count == 0,
                "The Active Directory configuration is invalid.")
            .ValidateOnStart();

        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ILdapServiceAccountCredentialProvider, DpapiLdapServiceAccountCredentialProvider>();
        }

        services.AddScoped<ILdapService, LdapService>();
        services.AddScoped<ILdapAuthenticationClient, LdapAuthenticationClient>();
        services.AddScoped<IAuthenticationService, LdapAuthenticationService>();

        return services;
    }
}
