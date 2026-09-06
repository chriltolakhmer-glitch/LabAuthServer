using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public sealed class LdapAuthenticationClient : ILdapAuthenticationClient
{
    public async Task<LdapAuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        LdapOptions options,
        ILdapServiceAccountCredentialProvider credentialProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(credentialProvider);

        if (!options.UseLdaps || options.Port != 636)
        {
            throw new InvalidOperationException("LDAP authentication requires LDAPS on TCP port 636.");
        }

        var serviceAccountPassword = await credentialProvider.GetPasswordAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(serviceAccountPassword))
        {
            return new LdapAuthenticationResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.DirectoryUnavailable
            };
        }

        return await Task.Run(
            () => LookupAndBind(username, password, serviceAccountPassword, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<LdapBindResult> BindAsync(
        string username,
        string password,
        LdapOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.UseLdaps || options.Port != 636)
        {
            throw new InvalidOperationException("LDAP authentication requires LDAPS on TCP port 636.");
        }

        return await Task.Run(
            () => Bind(username, password, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private static LdapBindResult Bind(
        string username,
        string password,
        LdapOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new LdapConnection(new LdapDirectoryIdentifier(options.Host, 636))
        {
            Timeout = options.ConnectionTimeout
        };
        using var cancellationRegistration = cancellationToken.Register(connection.Dispose);

        try
        {
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = true;
            connection.Bind(new NetworkCredential(username, password));

            return new LdapBindResult
            {
                IsSuccess = true,
                FailureCategory = LdapBindFailureCategory.None
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.Cancelled
            };
        }
        catch (LdapException) when (cancellationToken.IsCancellationRequested)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.Cancelled
            };
        }
        catch (LdapException ex) when (ex.ErrorCode == 49)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.InvalidCredentials
            };
        }
        catch (LdapException ex) when (ex.ErrorCode == 81)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.DirectoryUnavailable
            };
        }
        catch (LdapException ex) when (ex.ErrorCode == 85)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.Timeout
            };
        }
        catch (TimeoutException)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.Timeout
            };
        }
        catch (LdapException)
        {
            return new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.Unexpected
            };
        }
    }

    private static LdapAuthenticationResult LookupAndBind(
        string username,
        string password,
        string serviceAccountPassword,
        LdapOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new LdapConnection(new LdapDirectoryIdentifier(options.Host, options.Port))
        {
            Timeout = options.ConnectionTimeout
        };
        using var cancellationRegistration = cancellationToken.Register(connection.Dispose);

        try
        {
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = true;
            connection.Bind(new NetworkCredential(options.ServiceAccountUsername, serviceAccountPassword));

            var searchRequest = new SearchRequest(
                options.UserSearchBaseDn,
                $"(&(objectCategory=person)(objectClass=user)(userPrincipalName={LdapFilterEscaper.Escape(username)}))",
                SearchScope.Subtree,
                new[] { "distinguishedName", "displayName", "userPrincipalName", "userAccountControl" });
            var searchResponse = connection.SendRequest(searchRequest) as SearchResponse;

            if (searchResponse is null || searchResponse.ResultCode != ResultCode.Success || searchResponse.Entries.Count != 1)
            {
                return InvalidCredentials();
            }

            var entry = searchResponse.Entries[0];
            if (IsDisabled(entry))
            {
                return InvalidCredentials();
            }

            var distinguishedName = entry.DistinguishedName;
            connection.Bind(new NetworkCredential(GetUserBindUsername(username, distinguishedName), password));

            return new LdapAuthenticationResult
            {
                IsSuccess = true,
                FailureCategory = LdapBindFailureCategory.None,
                Username = username,
                DistinguishedName = distinguishedName,
                DisplayName = GetAttribute(entry, "displayName"),
                UserPrincipalName = GetAttribute(entry, "userPrincipalName") ?? username
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.Cancelled };
        }
        catch (LdapException) when (cancellationToken.IsCancellationRequested)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.Cancelled };
        }
        catch (LdapException ex) when (ex.ErrorCode == 49)
        {
            return InvalidCredentials();
        }
        catch (LdapException ex) when (ex.ErrorCode == 81)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.DirectoryUnavailable };
        }
        catch (LdapException ex) when (ex.ErrorCode == 85)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.Timeout };
        }
        catch (TimeoutException)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.Timeout };
        }
        catch (LdapException)
        {
            return new LdapAuthenticationResult { IsSuccess = false, FailureCategory = LdapBindFailureCategory.Unexpected };
        }
    }

    private static bool IsDisabled(SearchResultEntry entry)
    {
        var value = GetAttribute(entry, "userAccountControl");
        return int.TryParse(value, out var accountControl) && (accountControl & 0x2) != 0;
    }

    internal static string GetUserBindUsername(string username, string distinguishedName)
        => username;

    private static string? GetAttribute(SearchResultEntry entry, string name)
    {
        return entry.Attributes.Contains(name) && entry.Attributes[name].Count > 0
            ? entry.Attributes[name][0]?.ToString()
            : null;
    }

    private static LdapAuthenticationResult InvalidCredentials()
    {
        return new LdapAuthenticationResult
        {
            IsSuccess = false,
            FailureCategory = LdapBindFailureCategory.InvalidCredentials
        };
    }
}
