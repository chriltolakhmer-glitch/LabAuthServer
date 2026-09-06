namespace LabAuthServer.Application.DTOs;

public sealed record TokenIssuanceRequest
{
    public required string Subject { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
}
