using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Services;

/// <summary>
/// Implementation of LDAP operations using System.DirectoryServices.Protocols.
/// </summary>
public sealed class LdapService : ILdapService
{
    private readonly IOptions<LdapOptions> _ldapOptions;
    private readonly ILdapServiceAccountCredentialProvider _credentialProvider;
    private readonly ILogger<LdapService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LdapService"/> class.
    /// </summary>
    /// <param name="ldapOptions">LDAP configuration options.</param>
    /// <param name="credentialProvider">LDAP service-account credential provider.</param>
    /// <param name="logger">Logger instance.</param>
    public LdapService(
        IOptions<LdapOptions> ldapOptions,
        ILdapServiceAccountCredentialProvider credentialProvider,
        ILogger<LdapService> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapOptions);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _ldapOptions = ldapOptions;
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    /// <summary>
    /// Queries the Root DSE (Directory Server Entry) of the LDAP directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the Root DSE result with connectivity status and attributes.</returns>
    public async Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Root DSE query to {Host}:{Port}", _ldapOptions.Value.Host, _ldapOptions.Value.Port);

            var result = await Task.Run(() => PerformRootDseQuery(), cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Root DSE query succeeded for {Host}:{Port}", _ldapOptions.Value.Host, _ldapOptions.Value.Port);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Root DSE query was cancelled");
            return new RootDseResult
            {
                IsSuccess = false,
                ErrorMessage = "The operation was cancelled."
            };
        }
        catch (LdapException ex)
        {
            _logger.LogError("LDAP error during Root DSE query: {ErrorCode}", ex.ErrorCode);
            return new RootDseResult
            {
                IsSuccess = false,
                ErrorMessage = "The directory service could not complete the Root DSE query."
            };
        }
        catch (Exception)
        {
            _logger.LogError("Unexpected error during Root DSE query");
            return new RootDseResult
            {
                IsSuccess = false,
                ErrorMessage = "The directory service could not complete the Root DSE query."
            };
        }
    }

    public async Task<IReadOnlyList<string>> GetUserGroupsAsync(
        string userPrincipalName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrincipalName) || string.IsNullOrWhiteSpace(password))
        {
            return Array.Empty<string>();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Querying AD group memberships for {UserPrincipalName}", userPrincipalName);

            var groups = await Task.Run(
                () => PerformUserGroupQuery(userPrincipalName, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Resolved {GroupCount} AD group memberships for {UserPrincipalName}", groups.Count, userPrincipalName);
            return groups;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AD group membership query was cancelled for {UserPrincipalName}", userPrincipalName);
            return Array.Empty<string>();
        }
        catch (LdapException ex)
        {
            _logger.LogError("LDAP error while resolving AD group memberships for {UserPrincipalName}: {ErrorCode}", userPrincipalName, ex.ErrorCode);
            return Array.Empty<string>();
        }
        catch (Exception)
        {
            _logger.LogError("Unexpected error while resolving AD group memberships for {UserPrincipalName}", userPrincipalName);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Performs the Root DSE query (blocking operation).
    /// </summary>
    private RootDseResult PerformRootDseQuery()
    {
        var options = _ldapOptions.Value;

        // Create LDAP directory identifier
        var directoryIdentifier = new LdapDirectoryIdentifier(options.Host, options.Port);

        // Create connection with LDAPS if configured
        var connection = new LdapConnection(directoryIdentifier)
        {
            Timeout = options.ConnectionTimeout
        };

        try
        {
            // Configure session options
            connection.SessionOptions.ProtocolVersion = 3; // LDAP v3
            connection.SessionOptions.SecureSocketLayer = options.UseLdaps;

            var serviceAccountUsername = options.ServiceAccountUsername?.Trim();
            if (string.IsNullOrWhiteSpace(serviceAccountUsername))
            {
                throw new InvalidOperationException(
                    "An LDAP service account is required for the Root DSE query. Configure ActiveDirectory:ServiceAccountUsername through secure configuration.");
            }

            var serviceAccountPassword = _credentialProvider.GetPasswordAsync().GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(serviceAccountPassword))
            {
                throw new InvalidOperationException("LDAP service-account password is not configured.");
            }

            connection.Bind(new NetworkCredential(serviceAccountUsername, serviceAccountPassword));

            // Query RootDSE (empty DN and scope = Base)
            var searchRequest = new SearchRequest(
                distinguishedName: null, // Root DSE
                ldapFilter: "(objectClass=*)", // Match root object
                searchScope: SearchScope.Base,
                attributeList: null); // All attributes

            var searchResponse = connection.SendRequest(searchRequest) as SearchResponse;

            if (searchResponse == null)
            {
                return new RootDseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No response received from server."
                };
            }

            if (searchResponse.ResultCode != ResultCode.Success)
            {
                return new RootDseResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"LDAP search failed: {searchResponse.ResultCode}"
                };
            }

            // Extract Root DSE entry
            if (searchResponse.Entries.Count == 0)
            {
                return new RootDseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Root DSE not found in response."
                };
            }

            var rootDseEntry = searchResponse.Entries[0];
            var attributes = ExtractAttributes(rootDseEntry);

            return new RootDseResult
            {
                IsSuccess = true,
                Attributes = attributes
            };
        }
        finally
        {
            connection?.Dispose();
        }
    }

    private IReadOnlyList<string> PerformUserGroupQuery(
        string userPrincipalName,
        CancellationToken cancellationToken)
    {
        var options = _ldapOptions.Value;
        if (!options.UseLdaps || options.Port != 636)
        {
            return Array.Empty<string>();
        }

        var directoryIdentifier = new LdapDirectoryIdentifier(options.Host, options.Port);

        using var connection = new LdapConnection(directoryIdentifier)
        {
            Timeout = options.ConnectionTimeout
        };
        using var cancellationRegistration = cancellationToken.Register(connection.Dispose);

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = true;

        var serviceAccountUsername = options.ServiceAccountUsername?.Trim();
        if (string.IsNullOrWhiteSpace(serviceAccountUsername))
        {
            return Array.Empty<string>();
        }

        var serviceAccountPassword = _credentialProvider.GetPasswordAsync(cancellationToken).GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(serviceAccountPassword))
        {
            return Array.Empty<string>();
        }

        connection.Bind(new NetworkCredential(serviceAccountUsername, serviceAccountPassword));

        var searchRequest = new SearchRequest(
            options.UserSearchBaseDn,
            $"(&(objectClass=user)(userPrincipalName={LdapFilterEscaper.Escape(userPrincipalName)}))",
            SearchScope.Subtree,
            new[] { "memberOf" });

        var searchResponse = connection.SendRequest(searchRequest) as SearchResponse;
        if (searchResponse is null || searchResponse.ResultCode != ResultCode.Success)
        {
            return Array.Empty<string>();
        }

        var groupNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (SearchResultEntry entry in searchResponse.Entries)
        {
            if (!entry.Attributes.Contains("memberOf"))
            {
                continue;
            }

            foreach (var attribute in entry.Attributes["memberOf"].GetValues(typeof(string)))
            {
                var groupName = GetGroupNameFromDistinguishedName(attribute?.ToString());
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    groupNames.Add(groupName.Trim());
                }
            }
        }

        return groupNames.ToArray();
    }

    private static string? GetGroupNameFromDistinguishedName(string? distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName))
        {
            return null;
        }

        foreach (var segment in distinguishedName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmedValue = segment.Trim();
            if (trimmedValue.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedValue[3..].Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts attributes from a directory entry into a read-only dictionary.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ExtractAttributes(SearchResultEntry entry)
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (DirectoryAttribute attribute in entry.Attributes.Values)
        {
            var values = new List<string>();

            foreach (var value in attribute)
            {
                if (value is byte[] byteArray)
                {
                    // Handle binary attributes
                    values.Add($"[Binary: {byteArray.Length} bytes]");
                }
                else if (value is string stringValue)
                {
                    values.Add(stringValue);
                }
                else
                {
                    values.Add(value?.ToString() ?? "[null]");
                }
            }

            attributes[attribute.Name] = values.AsReadOnly();
        }

        return attributes;
    }
}
