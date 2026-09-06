using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Constants;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using JwtClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace LabAuthServer.Api.Extensions;

public sealed class JwtBearerAuthenticationOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IOptions<TokenOptions> _tokenOptions;
    private readonly IProtectedSigningKeyProvider _keyProvider;

    public JwtBearerAuthenticationOptions(
        IOptions<TokenOptions> tokenOptions,
        IProtectedSigningKeyProvider keyProvider)
    {
        _tokenOptions = tokenOptions ?? throw new ArgumentNullException(nameof(tokenOptions));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        var tokenOptions = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(tokenOptions);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"JWT validation configuration is invalid: {string.Join("; ", failures)}");
        }

        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tokenOptions.Issuer,
            IssuerValidator = ValidateIssuerAndRequiredClaims,
            ValidateAudience = true,
            ValidAudience = tokenOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = tokenOptions.ClockSkew,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ValidateActor = false,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidAlgorithms = [tokenOptions.SigningAlgorithm],
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                if (string.IsNullOrWhiteSpace(kid))
                {
                    throw new SecurityTokenException("The JWT is missing a key identifier ('kid').");
                }

                var keyMaterial = _keyProvider.GetKeyAsync(kid).GetAwaiter().GetResult();
                using var _ = keyMaterial;
                var publicKey = RSA.Create(keyMaterial.PrivateKey.ExportParameters(false));
                return new[] { new RsaSecurityKey(publicKey) };
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateRequiredClaimsAsync,
            OnAuthenticationFailed = AuditAuthenticationFailureAsync
        };
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(null, options);
    }

    public static Task ValidateRequiredClaimsAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var requiredClaims = new[]
        {
                    JwtClaimNames.Sub,
                    JwtClaimNames.Jti,
                    JwtClaimNames.Iat,
                    JwtClaimNames.Nbf
        };

        if (principal is null || requiredClaims.Any(claimType =>
                principal.Claims.Count(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal)) != 1))
        {
            context.Fail("JWT required claims are invalid.");
            return Task.CompletedTask;
        }

        var roles = principal.Claims
            .Where(claim => string.Equals(claim.Type, "role", StringComparison.Ordinal))
            .Select(claim => claim.Value)
            .ToArray();

        if (roles.Length != 1 || !AuthorizationRoles.All.Contains(roles[0], StringComparer.Ordinal))
        {
            context.Fail("JWT role claim is invalid.");
        }

        return Task.CompletedTask;
    }


    private static async Task AuditAuthenticationFailureAsync(AuthenticationFailedContext context)
    {
        var eventType = ClassifyAuthenticationFailure(context.Exception);
        var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerAuthenticationOptions>>();
        logger?.LogWarning("JWT bearer authentication failed with classification {FailureClassification}.", eventType);

        var audit = context.HttpContext.RequestServices.GetService<IAuditEventService>();
        var correlation = context.HttpContext.Items[Requests.CorrelationContext.ItemKey] as Requests.CorrelationContext;
        if (audit is null || correlation is null)
            return;

        try
        {
            await audit.WriteAsync(new AuditEvent
            {
                EventTypeCode = eventType,
                CorrelationId = correlation.CorrelationId,
                RequestId = correlation.RequestId,
                Endpoint = context.HttpContext.GetEndpoint()?.DisplayName ?? context.HttpContext.Request.Path.Value,
                HttpMethod = context.HttpContext.Request.Method,
                StatusCode = StatusCodes.Status401Unauthorized,
                Success = false,
                ClientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                ServerName = Environment.MachineName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString()
            }, context.HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            logger?.LogWarning("Audit event could not be persisted for a bearer authentication failure.");
        }
    }

    internal static string ClassifyAuthenticationFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            SecurityTokenExpiredException => AuditEventTypes.ExpiredToken,
            SecurityTokenInvalidSignatureException => AuditEventTypes.InvalidSignature,
            SecurityTokenInvalidIssuerException => AuditEventTypes.InvalidIssuer,
            SecurityTokenInvalidAudienceException => AuditEventTypes.InvalidAudience,
            _ when exception.Message.Contains("role", StringComparison.OrdinalIgnoreCase) => AuditEventTypes.InvalidRole,
            _ => AuditEventTypes.InvalidToken
        };
    }

    private static string ValidateIssuerAndRequiredClaims(
        string issuer,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        if (!string.Equals(issuer, validationParameters.ValidIssuer, StringComparison.Ordinal))
        {
            throw new SecurityTokenInvalidIssuerException("The JWT issuer is invalid.");
        }

        if (securityToken is not JwtSecurityToken and not JsonWebToken)
        {
            throw new SecurityTokenException("The JWT format is invalid.");
        }

        var claims = securityToken switch
        {
            JwtSecurityToken jwt => jwt.Claims.ToArray(),
            JsonWebToken jsonWebToken => jsonWebToken.Claims.ToArray(),
            _ => throw new SecurityTokenException("The JWT format is invalid.")
        };
        var requiredClaims = new[]
        {
            JwtClaimNames.Sub,
            JwtClaimNames.Jti,
            JwtClaimNames.Iat,
            JwtClaimNames.Nbf
        };

        if (requiredClaims.Any(claimType =>
                claims.Count(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal)) != 1))
        {
            throw new SecurityTokenException("The JWT required claims are invalid.");
        }

        var roles = claims
            .Where(claim => string.Equals(claim.Type, "role", StringComparison.Ordinal))
            .Select(claim => claim.Value)
            .ToArray();
        if (roles.Length != 1 || !AuthorizationRoles.All.Contains(roles[0], StringComparer.Ordinal))
        {
            throw new SecurityTokenException("The JWT role claim is invalid.");
        }

        return issuer;
    }
}
