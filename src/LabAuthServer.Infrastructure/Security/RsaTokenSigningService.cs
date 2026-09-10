using System.Security.Cryptography;
using System.Text.Json;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security;

public sealed class RsaTokenSigningService : ITokenSigningService
{
    private readonly IOptions<TokenOptions> _tokenOptions;
    private readonly IProtectedSigningKeyProvider _keyProvider;
    private readonly ILogger<RsaTokenSigningService> _logger;

    public RsaTokenSigningService(
        IOptions<TokenOptions> tokenOptions,
        IProtectedSigningKeyProvider keyProvider,
        ILogger<RsaTokenSigningService> logger)
    {
        _tokenOptions = tokenOptions ?? throw new ArgumentNullException(nameof(tokenOptions));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SignedToken> SignAsync(
        TokenClaims claims,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claims);
        cancellationToken.ThrowIfCancellationRequested();

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.ValidateForRuntime(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Signing configuration is invalid.");
        }

        var signingParameters = GetSigningParameters(options.SigningAlgorithm);
        using var keyMaterial = await _keyProvider.GetActiveKeyAsync(cancellationToken).ConfigureAwait(false);
        RsaKeySizePolicy.Validate(keyMaterial.PrivateKey);

        if (!string.Equals(keyMaterial.KeyIdentifier, options.ActiveKeyId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The active signing key does not match the configured key identifier.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var header = SerializeHeader(options.SigningAlgorithm, keyMaterial.KeyIdentifier);
        var payload = SerializeApprovedClaims(claims, options.MaximumClaimSize, options.MaximumTokenSize);
        var encodedHeader = Base64UrlEncode(header);
        var encodedPayload = Base64UrlEncode(payload);
        var signingInput = System.Text.Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
        var signature = keyMaterial.PrivateKey.SignData(
            signingInput,
            signingParameters.HashAlgorithm,
            signingParameters.Padding);

        _logger.LogDebug("Signed token claims with configured key identifier {KeyIdentifier}", options.ActiveKeyId);

        return new SignedToken
        {
            AccessToken = $"{encodedHeader}.{encodedPayload}.{Base64UrlEncode(signature)}",
            KeyIdentifier = keyMaterial.KeyIdentifier
        };
    }

    private static byte[] SerializeHeader(string signingAlgorithm, string keyIdentifier)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = signingAlgorithm,
            typ = "JWT",
            kid = keyIdentifier
        });
    }

    private static byte[] SerializeApprovedClaims(TokenClaims claims, int maximumClaimSize, int maximumTokenSize)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = claims.Issuer,
            aud = claims.Audience,
            sub = claims.Subject,
            jti = claims.Jti,
            iat = claims.IssuedAt.ToUnixTimeSeconds(),
            nbf = (claims.NotBefore ?? claims.IssuedAt).ToUnixTimeSeconds(),
            exp = claims.ExpiresAt.ToUnixTimeSeconds(),
            role = claims.Roles.Single(),
            scope = claims.Scopes
        });

        if (payload.Length > maximumTokenSize)
        {
            throw new InvalidOperationException("Signing payload exceeds the configured maximum token size.");
        }

        if (claims.Issuer.Length > maximumClaimSize ||
            claims.Audience.Length > maximumClaimSize ||
            claims.Subject.Length > maximumClaimSize)
        {
            throw new InvalidOperationException("A signing claim exceeds the configured maximum claim size.");
        }

        return payload;
    }

    private static (HashAlgorithmName HashAlgorithm, RSASignaturePadding Padding) GetSigningParameters(string algorithm)
    {
        return algorithm switch
        {
            "RS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "RS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            "RS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            "PS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            "PS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            "PS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            _ => throw new InvalidOperationException("The configured signing algorithm is not supported.")
        };
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
