using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;

namespace LabAuthServer.Application.Services;

public sealed class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly int _maximumClaimSize;
    private readonly int _maximumTokenSize;
    private readonly ITokenSigningService _tokenSigningService;

    public TokenService(
        string issuer,
        string audience,
        TimeSpan accessTokenLifetime,
        int maximumClaimSize,
        int maximumTokenSize,
        ITokenSigningService tokenSigningService)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
            !string.Equals(issuerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Token issuer must be an absolute HTTPS URI.", nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(audience) || audience.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Token audience is required and must not contain whitespace.", nameof(audience));
        }

        if (accessTokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(accessTokenLifetime), "Access-token lifetime must be greater than zero.");
        }

        if (maximumClaimSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumClaimSize), "Maximum claim size must be greater than zero.");
        }

        if (maximumTokenSize <= 0 || maximumTokenSize < maximumClaimSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumTokenSize), "Maximum token size must be at least the maximum claim size.");
        }

        _issuer = issuer;
        _audience = audience;
        _accessTokenLifetime = accessTokenLifetime;
        _maximumClaimSize = maximumClaimSize;
        _maximumTokenSize = maximumTokenSize;
        _tokenSigningService = tokenSigningService ?? throw new ArgumentNullException(nameof(tokenSigningService));
    }

    public async Task<TokenResponse> IssueAsync(
        TokenIssuanceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issuedAt = DateTimeOffset.UtcNow;
        var claimsBuilder = new TokenClaimsBuilder(
            _issuer,
            _audience,
            _accessTokenLifetime,
            _maximumClaimSize,
            _maximumTokenSize);

        var claims = claimsBuilder.Build(request, issuedAt);
        var signedToken = await _tokenSigningService.SignAsync(claims, cancellationToken).ConfigureAwait(false);

        return new TokenResponse
        {
            AccessToken = signedToken.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = claims.ExpiresAt
        };
    }
}
