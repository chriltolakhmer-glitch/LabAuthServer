namespace LabAuthServer.Application.DTOs;

public sealed record TokenResponse
{
    public required string AccessToken { get; init; }

    public required string TokenType { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
