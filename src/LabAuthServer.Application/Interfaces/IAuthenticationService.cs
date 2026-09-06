using LabAuthServer.Application.Enums;

namespace LabAuthServer.Application.Interfaces;

/// <summary>
/// Service for user authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user against the directory using the provided credentials.
    /// </summary>
    /// <param name="username">Username or user principal name (e.g., user@lab.local)</param>
    /// <param name="password">Password (used for LDAP bind only, not stored)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the authentication result with success status and error message (if failed)</returns>
    Task<AuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public sealed record AuthenticationResult
{
    /// <summary>
    /// Gets a value indicating whether authentication succeeded.
    /// </summary>
    public required bool IsAuthenticated { get; init; }

    public AuthenticationFailureCategory FailureCategory { get; init; }

    /// <summary>
    /// Gets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public string? Username { get; init; }

    public string? DistinguishedName { get; init; }

    public string? DisplayName { get; init; }

    public string? UserPrincipalName { get; init; }
}
