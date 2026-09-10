using LabAuthServer.Application.Services;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Services;

public sealed class LdapAuthenticationService : IAuthenticationService
{
    private readonly IOptions<LdapOptions> _ldapOptions;
    private readonly ILdapAuthenticationClient _ldapClient;
    private readonly ILdapServiceAccountCredentialProvider _credentialProvider;
    private readonly ILogger<LdapAuthenticationService> _logger;

    public LdapAuthenticationService(IOptions<LdapOptions> ldapOptions, ILdapAuthenticationClient ldapClient,
        ILdapServiceAccountCredentialProvider credentialProvider, ILogger<LdapAuthenticationService> logger)
    {
        _ldapOptions = ldapOptions ?? throw new ArgumentNullException(nameof(ldapOptions));
        _ldapClient = ldapClient ?? throw new ArgumentNullException(nameof(ldapClient));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
    {
        using var ownedOperation = operation is null ? new AuthenticationOperation(_ldapOptions.Value.AuthenticationTimeout, cancellationToken) : null;
        operation ??= ownedOperation!;
        var stage = DirectoryFailureStage.CredentialLoading;
        AuthenticationResult result;
        try
        {
            operation.EnterStage(stage);
            var options = _ldapOptions.Value;
            if (!options.UseLdaps || options.Port != 636)
                result = AuthenticationResult.Failed(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.InvalidConfiguration));
            else if (!IsValidUpn(username, options.Domain) || string.IsNullOrWhiteSpace(password))
                result = AuthenticationResult.Failed(new(AuthenticationFailureCategory.InvalidRequest, stage, DirectoryFailureReason.InvalidInput));
            else
            {
                result = await _ldapClient.AuthenticateAsync(username, password, options, _credentialProvider, operation.Token, operation).ConfigureAwait(false);
            }
            operation.ThrowIfCancellationRequested();
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, stage))
        {
            result = AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Classify(exception, stage, operation.Token));
        }
        catch (Exception)
        {
            result = AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Unexpected(stage));
        }
        if (result.Failure is not null) LdapFailureLogging.Write(_logger, result.Failure);
        // Identity verification is not complete login success. The controller
        // owns the success audit after groups, roles and token issuance succeed.
        return result;
    }

    private static bool IsValidUpn(string username, string domain)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(domain)) return false;
        var separator = username.IndexOf('@');
        return separator > 0 && separator == username.LastIndexOf('@') && separator < username.Length - 1
            && username[(separator + 1)..].Equals(domain, StringComparison.OrdinalIgnoreCase);
    }
}
