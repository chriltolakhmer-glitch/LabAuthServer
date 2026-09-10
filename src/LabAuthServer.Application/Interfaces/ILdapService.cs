using LabAuthServer.Application.Services;
namespace LabAuthServer.Application.Interfaces;

/// <summary>
/// Service for LDAP operations.
/// </summary>
public interface ILdapService
{
    /// <summary>
    /// Queries the Root DSE (Directory Server Entry) of the LDAP directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the Root DSE result with connectivity status and attributes.</returns>
    Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the AD groups for the supplied user principal name using service-account credentials.
    /// </summary>
    /// <param name="userPrincipalName">User principal name to resolve.</param>
    /// <param name="password">Compatibility parameter; validated for presence, not used for the group bind.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="operation">Optional shared login budget; must be the same operation used for identity verification. The caller owns its lifetime.</param>
    /// <returns>The approved AD group names for the user.</returns>
    Task<LabAuthServer.Application.DTOs.GroupLookupResult> GetUserGroupsAsync(
        string userPrincipalName,
        string password,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null);
}

/// <summary>
/// Result of a Root DSE query operation.
/// </summary>
public sealed record RootDseResult
{
    /// <summary>
    /// Gets a value indicating whether the Root DSE query succeeded.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the query failed; otherwise null.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the Root DSE attributes returned by the server.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Attributes { get; init; }
}
