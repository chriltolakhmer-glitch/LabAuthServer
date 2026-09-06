namespace LabAuthServer.Application.DTOs;

public sealed record TokenClaims
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Subject { get; init; }

    public required string Jti { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }

    public DateTimeOffset? NotBefore { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
}
