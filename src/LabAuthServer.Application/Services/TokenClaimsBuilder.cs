using System.Text;
using LabAuthServer.Application.Constants;
using LabAuthServer.Application.DTOs;

namespace LabAuthServer.Application.Services;

public sealed class TokenClaimsBuilder
{
    private const int MinimumLifetimeSeconds = 1;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly int _maximumClaimSize;
    private readonly int _maximumTokenSize;

    public TokenClaimsBuilder(
        string issuer,
        string audience,
        TimeSpan accessTokenLifetime,
        int maximumClaimSize,
        int maximumTokenSize)
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

        if (accessTokenLifetime < TimeSpan.FromSeconds(MinimumLifetimeSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(accessTokenLifetime), "Access-token lifetime must be positive.");
        }

        if (maximumClaimSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumClaimSize), "Maximum claim size must be positive.");
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
    }

    public TokenClaims Build(TokenIssuanceRequest request, DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Token subject is required.", nameof(request));
        }

        var roles = NormalizeValues(request.Roles, "role");
        if (roles.Count != 1 || !AuthorizationRoles.All.Contains(roles[0], StringComparer.Ordinal))
        {
            throw new ArgumentException("Exactly one approved application role is required.", nameof(request));
        }

        var scopes = NormalizeValues(request.Scopes, "scope");
        var expiresAt = issuedAt.Add(_accessTokenLifetime);
        var claims = new TokenClaims
        {
            Issuer = _issuer,
            Audience = _audience,
            Subject = request.Subject,
            Jti = Guid.NewGuid().ToString("N"),
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Roles = roles,
            Scopes = scopes
        };

        ValidateClaimSizes(claims);
        return claims;
    }

    private IReadOnlyList<string> NormalizeValues(IReadOnlyList<string> values, string claimName)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalizedValues = values
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (normalizedValues.Any(value => Encoding.UTF8.GetByteCount(value) > _maximumClaimSize))
        {
            throw new ArgumentException($"A {claimName} claim exceeds the configured maximum size.");
        }

        return normalizedValues;
    }

    private void ValidateClaimSizes(TokenClaims claims)
    {
        var claimValues = new[]
        {
            claims.Issuer,
            claims.Audience,
            claims.Subject,
            string.Join(' ', claims.Roles),
            string.Join(' ', claims.Scopes)
        };

        var estimatedSize = claimValues.Sum(value => Encoding.UTF8.GetByteCount(value));
        if (estimatedSize > _maximumTokenSize)
        {
            throw new ArgumentException("Token claims exceed the configured maximum token size.");
        }
    }
}
