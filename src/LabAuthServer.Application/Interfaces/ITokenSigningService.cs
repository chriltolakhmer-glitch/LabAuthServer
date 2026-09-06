using LabAuthServer.Application.DTOs;

namespace LabAuthServer.Application.Interfaces;

public interface ITokenSigningService
{
    Task<SignedToken> SignAsync(
        TokenClaims claims,
        CancellationToken cancellationToken = default);
}

public sealed record SignedToken
{
    public required string AccessToken { get; init; }

    public required string KeyIdentifier { get; init; }
}
