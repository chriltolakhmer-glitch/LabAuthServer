using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;

namespace LabAuthServer.UnitTests;

public sealed class TokenServiceTests
{
    [Fact]
    public async Task IssueAsync_WithValidRequest_ReturnsApprovedBearerResponse()
    {
        var signer = new FakeTokenSigningService();
        var service = CreateService(signer);

        var response = await service.IssueAsync(new TokenIssuanceRequest
        {
            Subject = "user-123",
            Roles = ["Reader"],
            Scopes = ["reports.read"]
        });

        Assert.Equal("signed-token", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal("user-123", signer.LastClaims.Subject);
        Assert.Equal(["Reader"], signer.LastClaims.Roles);
        Assert.Equal(["reports.read"], signer.LastClaims.Scopes);
    }

    [Fact]
    public async Task IssueAsync_WithBlankSubject_ThrowsArgumentException()
    {
        var signer = new FakeTokenSigningService();
        var service = CreateService(signer);

        await Assert.ThrowsAsync<ArgumentException>(() => service.IssueAsync(new TokenIssuanceRequest
        {
            Subject = " "
        }));
    }

    [Fact]
    public async Task IssueAsync_WithoutApprovedRole_FailsClosed()
    {
        var service = CreateService(new FakeTokenSigningService());

        await Assert.ThrowsAsync<ArgumentException>(() => service.IssueAsync(new TokenIssuanceRequest
        {
            Subject = "user-123"
        }));
    }

    [Fact]
    public async Task IssueAsync_WithInvalidRole_FailsClosed()
    {
        var service = CreateService(new FakeTokenSigningService());

        await Assert.ThrowsAsync<ArgumentException>(() => service.IssueAsync(new TokenIssuanceRequest
        {
            Subject = "user-123",
            Roles = ["SuperUser"]
        }));
    }

    [Fact]
    public async Task IssueAsync_WhenSignerFails_PropagatesFailure()
    {
        var signer = new FailingTokenSigningService();
        var service = CreateService(signer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.IssueAsync(new TokenIssuanceRequest
        {
            Subject = "user-123",
            Roles = ["Reader"]
        }));
    }

    private static TokenService CreateService(ITokenSigningService signer)
    {
        return new TokenService(
            "https://issuer.example",
            "labauthserver-api",
            TimeSpan.FromMinutes(15),
            4096,
            16384,
            signer);
    }

    private sealed class FakeTokenSigningService : ITokenSigningService
    {
        public TokenClaims LastClaims { get; private set; } = null!;

        public Task<SignedToken> SignAsync(TokenClaims claims, CancellationToken cancellationToken = default)
        {
            LastClaims = claims;
            return Task.FromResult(new SignedToken
            {
                AccessToken = "signed-token",
                KeyIdentifier = "development-key-1"
            });
        }
    }

    private sealed class FailingTokenSigningService : ITokenSigningService
    {
        public Task<SignedToken> SignAsync(TokenClaims claims, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Signing failed.");
        }
    }
}
