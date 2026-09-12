using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Security;
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LabAuthServer.IntegrationTests;

/// <summary>Default test host. Scenario-specific fakes can override these safe boundaries.</summary>
public class InfrastructureSafeApiFactory : WebApplicationFactory<Program>
{
    public RecordingAuditEventService Audit { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditEventService>();
            services.AddSingleton<IAuditEventService>(Audit);
            services.RemoveAll<ILdapServiceAccountCredentialProvider>();
            services.AddSingleton<ILdapServiceAccountCredentialProvider, UnavailableTestCredentials>();
            services.RemoveAll<ILdapConnectionFactory>();
            services.AddSingleton<ILdapConnectionFactory, ForbiddenLdapConnections>();
            services.RemoveAll<IProtectedSigningKeyProvider>();
            services.AddSingleton<IProtectedSigningKeyProvider, ForbiddenHostSigningKeys>();

            // No environment-provided license file or trusted-key input is read at host startup.
            services.PostConfigure<LicenseValidationOptions>(options =>
            {
                options.LicenseFilePath = string.Empty;
                options.TrustedKeys.Clear();
            });
        });
    }

    private sealed class UnavailableTestCredentials : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Host credentials are unavailable in ordinary API tests; supply a scenario fake.");
    }

    private sealed class ForbiddenLdapConnections : ILdapConnectionFactory
    {
        public ILdapConnection Create(LdapOptions options)
            => throw new InvalidOperationException("Real LDAP connections are forbidden in ordinary API tests; supply a scenario fake.");
    }

    private sealed class ForbiddenHostSigningKeys : IProtectedSigningKeyProvider
    {
        public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Host signing keys are forbidden in ordinary API tests; supply a synthetic provider.");

        public ValueTask<ValidationKeyMaterial> GetValidationKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Host validation keys are forbidden in ordinary API tests; supply a synthetic provider.");
    }
}
