using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using LabAuthServer.Api.Extensions;
using LabAuthServer.Application.Constants;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LabAuthServer.IntegrationTests;

public sealed class ProtectedEndpointTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ProtectedEndpointTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProtected_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_RemainsAnonymous()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUnmatchedRoute_RemainsNotFound()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/not-a-real-endpoint");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void AuthorizationFallback_RequiresAuthenticatedUsers()
    {
        var options = _factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(options.FallbackPolicy!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task GetProtected_WithInvalidToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Basic credentials")]
    [InlineData("Bearer")]
    [InlineData("Bearer one two")]
    public async Task GetProtected_WithMalformedAuthorizationHeader_ReturnsUnauthorized(string authorization)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithExpiredToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(DateTime.UtcNow.AddMinutes(-10), null, issuedAt: DateTime.UtcNow.AddMinutes(-20)));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithoutRole_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(DateTime.UtcNow.AddMinutes(10)));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithReaderRole_ReturnsAuthenticatedResource()
    {
        using var client = _factory.CreateClient();
        var token = CreateToken(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Reader);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var jwtOptions = _factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, jwtOptions.TokenValidationParameters, out _);

        var response = await client.GetAsync("/api/v1/protected");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("authenticated", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user-123", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProtected_WithOperatorRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Operator));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithUnknownKey_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Reader, "unknown-key"));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithWrongIssuer_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Reader, issuer: "https://wrong.example"));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithWrongAudience_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Reader, audience: "wrong-audience"));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithMissingJti_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateTokenWithoutJti(DateTime.UtcNow.AddMinutes(10), AuthorizationRoles.Reader));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithInvalidRole_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), "UnapprovedRole"));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("subject")]
    public async Task GetProtected_WithMissingRequiredIdentityClaim_ReturnsUnauthorized(string missingClaim)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(
                DateTime.UtcNow.AddMinutes(10),
                AuthorizationRoles.Reader,
                includeSubject: missingClaim != "subject",
                includeIssuer: missingClaim != "issuer",
                includeAudience: missingClaim != "audience"));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProtected_WithMultipleRoleClaims_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(DateTime.UtcNow.AddMinutes(10), roles: [AuthorizationRoles.Reader, AuthorizationRoles.Operator]));

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string CreateToken(
        DateTime expiresAt,
        string? role = null,
        string keyIdentifier = TestApiFactory.ActiveKeyId,
        string issuer = TestApiFactory.Issuer,
        string audience = TestApiFactory.Audience,
        DateTime? issuedAt = null,
        bool includeSubject = true,
        bool includeIssuer = true,
        bool includeAudience = true,
        IReadOnlyCollection<string>? roles = null)
    {
        using var signingKey = RSA.Create(_factory.SigningKey.ExportParameters(true));
        var claims = new List<Claim>();
        if (includeSubject)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, "user-123"));
        }

        foreach (var roleValue in roles ?? (role is null ? Array.Empty<string>() : [role]))
        {
            claims.Add(new Claim("role", roleValue));
        }
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = includeIssuer ? issuer : null,
            Audience = includeAudience ? audience : null,
            IssuedAt = issuedAt ?? DateTime.UtcNow.AddMinutes(-1),
            NotBefore = issuedAt ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingKey) { KeyId = keyIdentifier },
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            }
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }

    private string CreateTokenWithoutJti(DateTime expiresAt, string role)
    {
        using var signingKey = RSA.Create(_factory.SigningKey.ExportParameters(true));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user-123"),
            new Claim("role", role)
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = TestApiFactory.Issuer,
            Audience = TestApiFactory.Audience,
            IssuedAt = DateTime.UtcNow.AddMinutes(-1),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingKey) { KeyId = TestApiFactory.ActiveKeyId },
                SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            }
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateJwtSecurityToken(descriptor));
    }
}

public sealed class TestApiFactory : InfrastructureSafeApiFactory
{
    public TestApiFactory() => ClientOptions.BaseAddress = new Uri("https://DC01.lab.local");
    public const string ActiveKeyId = "integration-test-key";
    public const string Issuer = "https://integration-test.example";
    public const string Audience = "LabAuthServer.API";

    public RSA SigningKey { get; } = RSA.Create(2048);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProtectedSigningKeyProvider>();
            services.AddSingleton<IProtectedSigningKeyProvider>(
                new ProtectedSigningKeyProvider(
                    Options.Create(new TokenOptions
                    {
                        Issuer = Issuer,
                        Audience = Audience,
                        AccessTokenLifetime = TimeSpan.FromMinutes(15),
                        ClockSkew = TimeSpan.FromMinutes(1),
                        SigningAlgorithm = "RS256",
                        ActiveKeyId = ActiveKeyId,
                        SigningKeyStoreReference = "integration-test-key",
                        MaximumClaimSize = 4096,
                        MaximumTokenSize = 16384
                    }),
                    new[] { new KeyValuePair<string, RSA>(ActiveKeyId, SigningKey) }));

            services.Configure<TokenOptions>(options =>
            {
                options.Issuer = Issuer;
                options.Audience = Audience;
                options.AccessTokenLifetime = TimeSpan.FromMinutes(15);
                options.ClockSkew = TimeSpan.FromMinutes(1);
                options.SigningAlgorithm = "RS256";
                options.ActiveKeyId = ActiveKeyId;
                options.SigningCertificateStoreLocation = string.Empty;
                options.SigningCertificateStoreName = string.Empty;
                options.SigningCertificateThumbprint = string.Empty;
                options.PreviousSigningCertificateThumbprint = string.Empty;
                options.SigningKeyStoreReference = "integration-test-key";
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = JwtBearerAuthenticationOptions.ValidateRequiredClaimsAsync
                };
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
                options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, kid, _) =>
                {
                    if (!string.Equals(kid, ActiveKeyId, StringComparison.Ordinal))
                    {
                        return Array.Empty<SecurityKey>();
                    }

                    return new[]
                    {
                        new RsaSecurityKey(RSA.Create(SigningKey.ExportParameters(false)))
                    };
                };
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SigningKey.Dispose();
        }

        base.Dispose(disposing);
    }
}
