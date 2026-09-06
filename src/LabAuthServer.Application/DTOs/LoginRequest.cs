using System.ComponentModel.DataAnnotations;

namespace LabAuthServer.Application.DTOs;

/// <summary>
/// Request model for user authentication via POST /api/v1/auth/login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Gets the username or user principal name (e.g., user@lab.local).
    /// </summary>
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(
        maximumLength: 1024,
        MinimumLength = 1,
        ErrorMessage = "Username must be between 1 and 1024 characters.")]
    public required string Username { get; init; }

    /// <summary>
    /// Gets the password (used for LDAP bind only; never stored).
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
        maximumLength: 256,
        MinimumLength = 1,
        ErrorMessage = "Password must be between 1 and 256 characters.")]
    public required string Password { get; init; }
}
