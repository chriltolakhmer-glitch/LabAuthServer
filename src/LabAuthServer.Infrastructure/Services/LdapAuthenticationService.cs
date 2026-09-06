using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Services;

/// <summary>
/// Implementation of user authentication using LDAP over LDAPS.
/// </summary>
public sealed class LdapAuthenticationService : IAuthenticationService
{
    private readonly IOptions<LdapOptions> _ldapOptions;
    private readonly ILdapAuthenticationClient _ldapClient;
    private readonly ILdapServiceAccountCredentialProvider _credentialProvider;
    private readonly ILogger<LdapAuthenticationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LdapAuthenticationService"/> class.
    /// </summary>
    /// <param name="ldapOptions">LDAP configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public LdapAuthenticationService(
        IOptions<LdapOptions> ldapOptions,
        ILdapAuthenticationClient ldapClient,
        ILdapServiceAccountCredentialProvider credentialProvider,
        ILogger<LdapAuthenticationService> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _ldapOptions = ldapOptions;
        _ldapClient = ldapClient ?? throw new ArgumentNullException(nameof(ldapClient));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user against the directory using the provided credentials.
    /// </summary>
    /// <param name="username">Username or user principal name (e.g., user@lab.local)</param>
    /// <param name="password">Password (used for LDAP bind only, not stored)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the authentication result with success status and error message (if failed)</returns>
    public async Task<AuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = _ldapOptions.Value;

            if (!options.UseLdaps || options.Port != 636)
            {
                return Failure(
                    AuthenticationFailureCategory.Configuration,
                    "Authentication service unavailable.");
            }

            if (!IsValidUpn(username, options.Domain))
            {
                return Failure(AuthenticationFailureCategory.InvalidRequest, "Authentication failed.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return Failure(AuthenticationFailureCategory.InvalidRequest, "Authentication failed.");
            }

            _logger.LogInformation("Authentication attempt for {Username}", username);

                var bindResult = await _ldapClient.AuthenticateAsync(
                    username,
                    password,
                    options,
                    _credentialProvider,
                    cancellationToken)
                .ConfigureAwait(false);

            var result = bindResult.IsSuccess
                ? new AuthenticationResult
                {
                    IsAuthenticated = true,
                    FailureCategory = AuthenticationFailureCategory.None,
                    Username = bindResult.Username,
                    DistinguishedName = bindResult.DistinguishedName,
                    DisplayName = bindResult.DisplayName,
                    UserPrincipalName = bindResult.UserPrincipalName
                }
                : Failure(MapFailureCategory(bindResult.FailureCategory), GetFailureMessage(bindResult.FailureCategory));

            if (result.IsAuthenticated)
            {
                _logger.LogInformation("Authentication succeeded for {Username}", username);
            }
            else
            {
                _logger.LogInformation("Authentication failed for {Username}", username);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Authentication attempt cancelled for {Username}", username);
            return Failure(AuthenticationFailureCategory.Cancelled, "Authentication request was cancelled.");
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("Invalid LDAP authentication configuration");
            return Failure(AuthenticationFailureCategory.Configuration, "Authentication service unavailable.");
        }
        catch (Exception)
        {
            _logger.LogError("Unexpected error during authentication for {Username}", username);
            return Failure(AuthenticationFailureCategory.Unexpected, "Authentication error.");
        }
    }

    private static bool IsValidUpn(string username, string domain)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var separatorIndex = username.IndexOf('@');
        return separatorIndex > 0 &&
            separatorIndex == username.LastIndexOf('@') &&
            separatorIndex < username.Length - 1 &&
            username[(separatorIndex + 1)..].Equals(domain, StringComparison.OrdinalIgnoreCase);
    }

    private static AuthenticationFailureCategory MapFailureCategory(LdapBindFailureCategory category)
    {
        return category switch
        {
            LdapBindFailureCategory.InvalidCredentials => AuthenticationFailureCategory.InvalidCredentials,
            LdapBindFailureCategory.DirectoryUnavailable => AuthenticationFailureCategory.DirectoryUnavailable,
            LdapBindFailureCategory.Timeout => AuthenticationFailureCategory.Timeout,
            LdapBindFailureCategory.Cancelled => AuthenticationFailureCategory.Cancelled,
            _ => AuthenticationFailureCategory.Unexpected
        };
    }

    private static string GetFailureMessage(LdapBindFailureCategory category)
    {
        return category switch
        {
            LdapBindFailureCategory.InvalidCredentials => "Authentication failed.",
            LdapBindFailureCategory.DirectoryUnavailable => "Authentication service unavailable.",
            LdapBindFailureCategory.Timeout => "Authentication request timed out.",
            LdapBindFailureCategory.Cancelled => "Authentication request was cancelled.",
            _ => "Authentication error."
        };
    }

    private static AuthenticationResult Failure(AuthenticationFailureCategory category, string message)
    {
        return new AuthenticationResult
        {
            IsAuthenticated = false,
            FailureCategory = category,
            ErrorMessage = message
        };
    }
}
