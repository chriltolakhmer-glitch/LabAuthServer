using LabAuthServer.Application.Services;
using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public sealed class LdapAuthenticationClient(ILdapConnectionFactory connectionFactory) : ILdapAuthenticationClient
{
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, LdapOptions options,
        ILdapServiceAccountCredentialProvider credentialProvider, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        using var ownedOperation = operation is null ? new AuthenticationOperation(options.AuthenticationTimeout, cancellationToken) : null;
        operation ??= ownedOperation!;
        ArgumentNullException.ThrowIfNull(credentialProvider);
        var stage = DirectoryFailureStage.CredentialLoading;
        try
        {
            operation.ThrowIfCancellationRequested();
            if (!options.UseLdaps || options.Port != 636 || string.IsNullOrWhiteSpace(options.ServiceAccountUsername))
                return AuthenticationResult.Failed(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.InvalidConfiguration));
            var servicePassword = await credentialProvider.GetPasswordAsync(operation.Token).ConfigureAwait(false);
            operation.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(servicePassword))
                return AuthenticationResult.Failed(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.CredentialStoreUnavailable));
            stage = DirectoryFailureStage.ConnectionSetup;
            operation.EnterStage(stage);
            var result = await Task.Run(() => LookupAndBind(username, password, servicePassword, options, operation), operation.Token).ConfigureAwait(false);
            operation.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, stage))
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Classify(exception, stage, operation.Token));
        }
        catch (Exception)
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Unexpected(stage));
        }
    }

    public async Task<AuthenticationResult> BindAsync(string username, string password, LdapOptions options,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        using var ownedOperation = operation is null ? new AuthenticationOperation(options.AuthenticationTimeout, cancellationToken) : null;
        operation ??= ownedOperation!;
        try
        {
            operation.EnterStage(DirectoryFailureStage.ConnectionSetup);
            if (!options.UseLdaps || options.Port != 636)
                return AuthenticationResult.Failed(new(AuthenticationFailureCategory.Configuration, DirectoryFailureStage.ConnectionSetup, DirectoryFailureReason.InvalidConfiguration));
            var result = await Task.Run(() => Bind(username, password, options, operation), operation.Token).ConfigureAwait(false);
            operation.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, DirectoryFailureStage.ConnectionSetup))
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Classify(exception, DirectoryFailureStage.ConnectionSetup, operation.Token));
        }
        catch (Exception)
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Unexpected(DirectoryFailureStage.ConnectionSetup));
        }
    }

    private AuthenticationResult Bind(string username, string password, LdapOptions options, AuthenticationOperation operation)
    {
        var stage = DirectoryFailureStage.ConnectionSetup;
        try
        {
            operation.ThrowIfCancellationRequested();
            using var connection = connectionFactory.Create(options);
            operation.ThrowIfCancellationRequested();
            connection.ConfigureSession(true);
            operation.ThrowIfCancellationRequested();
            stage = DirectoryFailureStage.UserBind;
            operation.EnterStage(stage);
            connection.Bind(new NetworkCredential(username, password));
            operation.ThrowIfCancellationRequested();
            return AuthenticationResult.Succeeded(username);
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, stage))
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Classify(exception, stage, operation.Token));
        }
        catch (Exception)
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Unexpected(stage));
        }
    }

    private AuthenticationResult LookupAndBind(string username, string password, string servicePassword,
        LdapOptions options, AuthenticationOperation operation)
    {
        var stage = DirectoryFailureStage.ConnectionSetup;
        try
        {
            operation.ThrowIfCancellationRequested();
            using var connection = connectionFactory.Create(options);
            operation.ThrowIfCancellationRequested();
            connection.ConfigureSession(true);
            operation.ThrowIfCancellationRequested();
            stage = DirectoryFailureStage.ServiceBind;
            operation.EnterStage(stage);
            connection.Bind(new NetworkCredential(options.ServiceAccountUsername, servicePassword));
            operation.ThrowIfCancellationRequested();
            stage = DirectoryFailureStage.UserSearch;
            operation.EnterStage(stage);
            var response = connection.Search(new SearchRequest(options.UserSearchBaseDn,
                $"(&(objectCategory=person)(objectClass=user)(userPrincipalName={LdapFilterEscaper.Escape(username)}))",
                SearchScope.Subtree, ["distinguishedName", "displayName", "userPrincipalName", "userAccountControl"]));
            operation.ThrowIfCancellationRequested();
            var failure = LdapResponseValidator.ValidateSearch(response, stage);
            if (failure is not null) return AuthenticationResult.Failed(failure);
            stage = DirectoryFailureStage.ResponseValidation;
            operation.EnterStage(stage);
            var entry = response!.Entries[0];
            var validIdentity = LdapResponseValidator.TryIdentity(entry, username, out var accountControl);
            operation.ThrowIfCancellationRequested();
            if (!validIdentity)
                return AuthenticationResult.Failed(new(AuthenticationFailureCategory.ProtocolFailure, stage, DirectoryFailureReason.InvalidIdentityData));
            if ((accountControl & 2) != 0)
                return AuthenticationResult.Failed(new(AuthenticationFailureCategory.InvalidCredentials, stage, DirectoryFailureReason.AccountDisabled));
            stage = DirectoryFailureStage.UserBind;
            operation.EnterStage(stage);
            connection.Bind(new NetworkCredential(GetUserBindUsername(username, entry.DistinguishedName), password));
            operation.ThrowIfCancellationRequested();
            var result = AuthenticationResult.Succeeded(username, entry.DistinguishedName,
                LdapResponseValidator.SingleAttribute(entry, "displayName"), LdapResponseValidator.SingleAttribute(entry, "userPrincipalName"));
            operation.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, stage))
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Classify(exception, stage, operation.Token));
        }
        catch (Exception)
        {
            return AuthenticationResult.Failed(operation.Failure ?? LdapFailureClassifier.Unexpected(stage));
        }
    }

    internal static string GetUserBindUsername(string username, string distinguishedName) => username;
}
