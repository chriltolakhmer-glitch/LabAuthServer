using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public interface ILdapAuthenticationClient
{
    Task<LdapAuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        LdapOptions options,
        ILdapServiceAccountCredentialProvider credentialProvider,
        CancellationToken cancellationToken = default);

    Task<LdapBindResult> BindAsync(
        string username,
        string password,
        LdapOptions options,
        CancellationToken cancellationToken = default);
}

public sealed record LdapAuthenticationResult
{
    public required bool IsSuccess { get; init; }

    public LdapBindFailureCategory FailureCategory { get; init; }

    public string? Username { get; init; }

    public string? DistinguishedName { get; init; }

    public string? DisplayName { get; init; }

    public string? UserPrincipalName { get; init; }
}

public sealed record LdapBindResult
{
    public required bool IsSuccess { get; init; }

    public LdapBindFailureCategory FailureCategory { get; init; }
}

public enum LdapBindFailureCategory
{
    None = 0,
    InvalidCredentials = 1,
    DirectoryUnavailable = 2,
    Timeout = 3,
    Cancelled = 4,
    Unexpected = 5
}
