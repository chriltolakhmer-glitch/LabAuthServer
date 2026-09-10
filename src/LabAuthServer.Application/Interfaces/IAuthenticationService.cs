using LabAuthServer.Application.Services;
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
    /// <param name="operation">Optional shared login budget. When supplied, its origin and token are authoritative; the caller owns its lifetime.</param>
    /// <returns>Task containing the authentication result with success status and error message (if failed)</returns>
    Task<AuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default, AuthenticationOperation? operation = null);
}
