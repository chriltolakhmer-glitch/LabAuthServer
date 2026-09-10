using LabAuthServer.Application.Services;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public interface ILdapAuthenticationClient
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, LdapOptions options,
        ILdapServiceAccountCredentialProvider credentialProvider, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null);

    Task<AuthenticationResult> BindAsync(string username, string password, LdapOptions options,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null);
}
