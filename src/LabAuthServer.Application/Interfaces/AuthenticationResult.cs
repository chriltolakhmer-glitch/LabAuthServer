using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;

namespace LabAuthServer.Application.Interfaces;

/// <summary>Immutable identity authentication outcome, independent of login completion.</summary>
public sealed class AuthenticationResult
{
    private AuthenticationResult(DirectoryFailure? failure, string? username = null,
        string? distinguishedName = null, string? displayName = null, string? userPrincipalName = null)
    {
        Failure = failure;
        Username = username;
        DistinguishedName = distinguishedName;
        DisplayName = displayName;
        UserPrincipalName = userPrincipalName;
    }

    public bool IsAuthenticated => Failure is null;
    public AuthenticationFailureCategory FailureCategory => Failure?.Category ?? AuthenticationFailureCategory.None;
    public DirectoryFailure? Failure { get; }
    public string? ErrorMessage => Failure?.SafeMessage;
    public string? Username { get; }
    public string? DistinguishedName { get; }
    public string? DisplayName { get; }
    public string? UserPrincipalName { get; }

    public static AuthenticationResult Succeeded(string username, string? distinguishedName = null,
        string? displayName = null, string? userPrincipalName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return new(null, username, distinguishedName, displayName, userPrincipalName);
    }

    public static AuthenticationResult Failed(DirectoryFailure failure)
        => new(failure ?? throw new ArgumentNullException(nameof(failure)));
}
