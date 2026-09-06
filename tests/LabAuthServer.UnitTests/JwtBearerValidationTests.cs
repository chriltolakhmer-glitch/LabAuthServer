using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LabAuthServer.Api.Extensions;
using LabAuthServer.Infrastructure.Security;

namespace LabAuthServer.UnitTests;

public sealed class JwtBearerValidationTests
{
    [Fact]
    public void JwtBearerOptions_RegistersCustomConfiguratorInOptionsPipeline()
    {
        var services = new ServiceCollection();

        services.AddTokenConfiguration(new ConfigurationBuilder().Build());

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>) &&
            descriptor.ImplementationType == typeof(JwtBearerAuthenticationOptions));
    }

    [Fact]
    public void JwtBearerOptions_ValidConfiguration_UsesApprovedIssuerAudienceAndAlgorithm()
    {
        var options = new JwtBearerOptions();
        var tokenOptions = CreateTokenOptions();
        var provider = new ProtectedSigningKeyProvider(Options.Create(tokenOptions), new[]
        {
            new KeyValuePair<string, RSA>(tokenOptions.ActiveKeyId, RSA.Create(2048))
        });

        var configurator = new JwtBearerAuthenticationOptions(Options.Create(tokenOptions), provider);
        configurator.Configure(options);

        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.Equal(tokenOptions.Issuer, options.TokenValidationParameters.ValidIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.Equal(tokenOptions.Audience, options.TokenValidationParameters.ValidAudience);
        Assert.Equal([tokenOptions.SigningAlgorithm], options.TokenValidationParameters.ValidAlgorithms);
    }

    [Fact]
    public void JwtBearerOptions_WithInvalidConfiguration_Throws()
    {
        var tokenOptions = CreateTokenOptions();
        tokenOptions.Issuer = "http://issuer.example";
        var provider = new ProtectedSigningKeyProvider(Options.Create(tokenOptions), new[]
        {
            new KeyValuePair<string, RSA>(tokenOptions.ActiveKeyId, RSA.Create(2048))
        });

        var configurator = new JwtBearerAuthenticationOptions(Options.Create(tokenOptions), provider);

        var exception = Assert.Throws<InvalidOperationException>(() => configurator.Configure(new JwtBearerOptions()));
        Assert.Contains("issuer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JwtBearerOptions_RejectsMissingKidByResolution()
    {
        var options = CreateTokenOptions();
        var provider = new ProtectedSigningKeyProvider(Options.Create(options), new[]
        {
            new KeyValuePair<string, RSA>(options.ActiveKeyId, RSA.Create(2048))
        });
        var jwtOptions = new JwtBearerAuthenticationOptions(Options.Create(options), provider);
        var target = new JwtBearerOptions();

        jwtOptions.Configure(target);

        var ex = Assert.Throws<SecurityTokenException>(() =>
            target.TokenValidationParameters.IssuerSigningKeyResolver(
                null,
                null,
                null,
                target.TokenValidationParameters));

        Assert.Contains("kid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JwtBearerOptions_ResolvesPublicKeyWithoutPrivateParameters()
    {
        var options = CreateTokenOptions();
        using var signingKey = RSA.Create(2048);
        var provider = new ProtectedSigningKeyProvider(Options.Create(options), new[]
        {
            new KeyValuePair<string, RSA>(options.ActiveKeyId, signingKey)
        });
        var configurator = new JwtBearerAuthenticationOptions(Options.Create(options), provider);
        var target = new JwtBearerOptions();

        configurator.Configure(target);

        var keys = target.TokenValidationParameters.IssuerSigningKeyResolver(
            null,
            null,
            options.ActiveKeyId,
            target.TokenValidationParameters);

        var rsaKey = Assert.IsType<RsaSecurityKey>(Assert.Single(keys));
        Assert.ThrowsAny<CryptographicException>(() => rsaKey.Rsa!.ExportParameters(true));
    }

    [Fact]
    public void JwtBearerOptions_ResolvesPreviousKeyDuringOverlapAndRejectsUnknownKey()
    {
        var options = CreateTokenOptions();
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = new ProtectedSigningKeyProvider(Options.Create(options), new[]
        {
            new KeyValuePair<string, RSA>(options.ActiveKeyId, activeKey),
            new KeyValuePair<string, RSA>(options.PreviousKeyId, previousKey)
        });
        var jwtOptions = new JwtBearerAuthenticationOptions(Options.Create(options), provider);
        var target = new JwtBearerOptions();

        jwtOptions.Configure(target);

        var previousResult = target.TokenValidationParameters.IssuerSigningKeyResolver(
            null,
            null,
            options.PreviousKeyId,
            target.TokenValidationParameters);
        Assert.Single(previousResult);
        Assert.Throws<InvalidOperationException>(() => target.TokenValidationParameters.IssuerSigningKeyResolver(
            null,
            null,
            "unknown-key",
            target.TokenValidationParameters));
    }

    [Fact]
    public void JwtBearerOptions_AcceptsJsonWebTokenInIssuerValidationCallback()
    {
        var tokenOptions = CreateTokenOptions();
        using var signingKey = RSA.Create(2048);
        var token = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = tokenOptions.Issuer,
            Audience = tokenOptions.Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "user-123"),
                new Claim(JwtRegisteredClaimNames.Jti, "test-jti"),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("role", "Reader")
            }),
            NotBefore = DateTime.UtcNow,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256)
        });
        var jsonWebToken = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
        var provider = new ProtectedSigningKeyProvider(Options.Create(tokenOptions), new[]
        {
            new KeyValuePair<string, RSA>(tokenOptions.ActiveKeyId, RSA.Create(2048))
        });
        var jwtOptions = new JwtBearerAuthenticationOptions(Options.Create(tokenOptions), provider);
        var target = new JwtBearerOptions();

        jwtOptions.Configure(target);

        var issuer = target.TokenValidationParameters.IssuerValidator!(
            tokenOptions.Issuer,
            jsonWebToken,
            target.TokenValidationParameters);

        Assert.Equal(tokenOptions.Issuer, issuer);
    }

    [Fact]
    public void JwtBearerOptions_RejectsUnsupportedTokenRepresentation()
    {
        var tokenOptions = CreateTokenOptions();
        var provider = new ProtectedSigningKeyProvider(Options.Create(tokenOptions), new[]
        {
            new KeyValuePair<string, RSA>(tokenOptions.ActiveKeyId, RSA.Create(2048))
        });
        var jwtOptions = new JwtBearerAuthenticationOptions(Options.Create(tokenOptions), provider);
        var target = new JwtBearerOptions();

        jwtOptions.Configure(target);

        var exception = Assert.Throws<SecurityTokenException>(() => target.TokenValidationParameters.IssuerValidator!(
            tokenOptions.Issuer,
            new UnsupportedSecurityToken(),
            target.TokenValidationParameters));

        Assert.Contains("format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(SecurityTokenExpiredException), "SEC_EXPIRED_TOKEN")]
    [InlineData(typeof(SecurityTokenInvalidSignatureException), "SEC_INVALID_SIGNATURE")]
    [InlineData(typeof(SecurityTokenInvalidIssuerException), "SEC_INVALID_ISSUER")]
    [InlineData(typeof(SecurityTokenInvalidAudienceException), "SEC_INVALID_AUDIENCE")]
    public void JwtBearerOptions_ClassifiesBearerFailuresSafely(
        Type exceptionType,
        string expectedEventType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "diagnostic-only")!;

        Assert.Equal(expectedEventType, JwtBearerAuthenticationOptions.ClassifyAuthenticationFailure(exception));
    }

    [Fact]
    public void JwtBearerOptions_ClassifiesRoleFailureWithoutExposingDetails()
    {
        var exception = new SecurityTokenException("JWT role claim is invalid; token=diagnostic-only");

        Assert.Equal("SEC_INVALID_ROLE", JwtBearerAuthenticationOptions.ClassifyAuthenticationFailure(exception));
    }

    private static TokenOptions CreateTokenOptions()
    {
        return new TokenOptions
        {
            Issuer = "https://issuer.example",
            Audience = "labauthserver-api",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            ClockSkew = TimeSpan.FromMinutes(5),
            SigningAlgorithm = "RS256",
            ActiveKeyId = "active-key-1",
            PreviousKeyId = "previous-key-1",
            PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            ApprovedKeyIds = ["active-key-1", "previous-key-1"],
            SigningKeyStoreReference = "environment://LabAuthServer/SigningKey",
            MaximumClaimSize = 4096,
            MaximumTokenSize = 16384
        };
    }

    private sealed class UnsupportedSecurityToken : SecurityToken
    {
        public override SecurityKey SecurityKey => null!;

        public override string Id => "unsupported";

        public override string Issuer => "https://issuer.example";

        public override SecurityKey? SigningKey { get; set; }

        public override DateTime ValidFrom => DateTime.UtcNow;

        public override DateTime ValidTo => DateTime.UtcNow.AddMinutes(15);
    }
}
