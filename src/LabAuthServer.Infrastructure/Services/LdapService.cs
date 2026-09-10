using LabAuthServer.Application.Services;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
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
    private readonly ILdapConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LdapService"/> class.
    /// </summary>
    /// <param name="ldapOptions">LDAP configuration options.</param>
    /// <param name="credentialProvider">LDAP service-account credential provider.</param>
    /// <param name="logger">Logger instance.</param>
    public LdapService(
        IOptions<LdapOptions> ldapOptions,
        ILdapServiceAccountCredentialProvider credentialProvider,
        ILogger<LdapService> logger, ILdapConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(ldapOptions);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _ldapOptions = ldapOptions;
        _credentialProvider = credentialProvider;
        _logger = logger;
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Starting Root DSE query to {Host}:{Port}", _ldapOptions.Value.Host, _ldapOptions.Value.Port);

            // Credential acquisition is awaited with the caller token before entering the
            // blocking provider call, so cancellation is not discarded by a synchronous wait.
            var serviceAccountUsername = _ldapOptions.Value.ServiceAccountUsername?.Trim();
            if (string.IsNullOrWhiteSpace(serviceAccountUsername))
            {
                throw new InvalidOperationException(
                    "An LDAP service account is required for the Root DSE query. Configure ActiveDirectory:ServiceAccountUsername through secure configuration.");
            }

            var serviceAccountPassword = await _credentialProvider.GetPasswordAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(serviceAccountPassword))
            {
                throw new InvalidOperationException("LDAP service-account password is not configured.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var result = await Task.Run(
                () => PerformRootDseQuery(serviceAccountUsername, serviceAccountPassword, cancellationToken),
                cancellationToken).ConfigureAwait(false);

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

    public async Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
    {
        using var ownedOperation = operation is null ? new AuthenticationOperation(_ldapOptions.Value.AuthenticationTimeout, cancellationToken) : null;
        operation ??= ownedOperation!;
        GroupLookupResult result;
        try
        {
            operation.EnterStage(DirectoryFailureStage.ConnectionSetup);
            result = string.IsNullOrWhiteSpace(userPrincipalName) || string.IsNullOrWhiteSpace(password)
                ? GroupFailure(new(AuthenticationFailureCategory.InvalidRequest, DirectoryFailureStage.GroupSearch, DirectoryFailureReason.InvalidInput))
                : await Task.Run(() => PerformUserGroupQuery(userPrincipalName, operation), operation.Token).ConfigureAwait(false);
            operation.ThrowIfCancellationRequested();
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, DirectoryFailureStage.GroupSearch))
        {
            result = GroupFailure(operation.Failure ?? LdapFailureClassifier.Classify(exception, DirectoryFailureStage.GroupSearch, operation.Token));
        }
        catch (Exception)
        {
            result = GroupFailure(operation.Failure ?? LdapFailureClassifier.Unexpected(DirectoryFailureStage.GroupSearch));
        }
        if (result.Failure is not null) LdapFailureLogging.Write(_logger, result.Failure);
        return result;
    }
    /// <summary>
    /// Performs the Root DSE query (blocking operation).
    /// </summary>
    private RootDseResult PerformRootDseQuery(string serviceAccountUsername, string serviceAccountPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

            cancellationToken.ThrowIfCancellationRequested();
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

    private GroupLookupResult PerformUserGroupQuery(string userPrincipalName, AuthenticationOperation operation)
    {
        var stage = DirectoryFailureStage.ConnectionSetup;
        try
        {
            var options = _ldapOptions.Value;
            if (!options.UseLdaps || options.Port != 636)
                return GroupFailure(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.InvalidConfiguration));
            operation.ThrowIfCancellationRequested();
            using var connection = _connectionFactory.Create(options);
            operation.ThrowIfCancellationRequested();
            connection.ConfigureSession(true);
            operation.ThrowIfCancellationRequested();
            stage = DirectoryFailureStage.CredentialLoading;
            operation.EnterStage(stage);
            var serviceUsername = options.ServiceAccountUsername?.Trim();
            if (string.IsNullOrWhiteSpace(serviceUsername))
                return GroupFailure(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.InvalidConfiguration));
            var servicePassword = _credentialProvider.GetPasswordAsync(operation.Token).GetAwaiter().GetResult();
            operation.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(servicePassword))
                return GroupFailure(new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.CredentialStoreUnavailable));
            stage = DirectoryFailureStage.ServiceBind;
            operation.EnterStage(stage);
            connection.Bind(new NetworkCredential(serviceUsername, servicePassword));
            operation.ThrowIfCancellationRequested();
            stage = DirectoryFailureStage.GroupSearch;
            operation.EnterStage(stage);
            var response = connection.Search(new SearchRequest(options.UserSearchBaseDn,
                $"(&(objectClass=user)(userPrincipalName={LdapFilterEscaper.Escape(userPrincipalName)}))",
                SearchScope.Subtree, ["memberOf"]));
            operation.ThrowIfCancellationRequested();
            var failure = LdapResponseValidator.ValidateSearch(response, stage);
            if (failure is not null) return GroupFailure(failure);
            stage = DirectoryFailureStage.ResponseValidation;
            operation.EnterStage(stage);
            var entry = response!.Entries[0];
            if (!LdapDistinguishedNameParser.TryParse(entry.DistinguishedName, out _))
                return GroupFailure(LdapResponseValidator.Invalid(DirectoryFailureReason.InvalidGroupData));
            // Ranged attributes cannot be interpreted as a complete membership set.
            if (entry.Attributes.Keys.Any(name => name.StartsWith("memberOf;", StringComparison.OrdinalIgnoreCase)))
                return GroupFailure(LdapResponseValidator.Invalid(DirectoryFailureReason.IncompleteMembership));
            var groups = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            if (entry.Attributes.TryGetValue("memberOf", out var memberships))
            {
                // Fail closed instead of truncating: an excess membership set must never be
                // silently shortened into a different authorization result.
                if (memberships.Count > options.MaximumGroupMemberships)
                    return GroupFailure(LdapResponseValidator.Invalid(DirectoryFailureReason.MembershipLimitExceeded));
                foreach (var membership in memberships)
                {
                    operation.ThrowIfCancellationRequested();
                    if (!LdapDistinguishedNameParser.TryParse(membership, out var name))
                        return GroupFailure(LdapResponseValidator.Invalid(DirectoryFailureReason.InvalidGroupData));
                    groups.Add(name);
                }
            }
            operation.ThrowIfCancellationRequested();
            return GroupLookupResult.Succeeded(groups.ToArray());
        }
        catch (Exception exception) when (LdapFailureClassifier.IsExpected(exception, stage))
        {
            return GroupFailure(operation.Failure ?? LdapFailureClassifier.Classify(exception, stage, operation.Token));
        }
        catch (Exception)
        {
            return GroupFailure(operation.Failure ?? LdapFailureClassifier.Unexpected(stage));
        }
    }

    private static GroupLookupResult GroupFailure(DirectoryFailure failure)
        => GroupLookupResult.Failed(failure.AtStage(failure.Reason is DirectoryFailureReason.CallerCancelled or DirectoryFailureReason.AuthenticationDeadlineExceeded ? failure.Stage : failure.Stage == DirectoryFailureStage.ResponseValidation
            ? DirectoryFailureStage.ResponseValidation : DirectoryFailureStage.GroupSearch));
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
