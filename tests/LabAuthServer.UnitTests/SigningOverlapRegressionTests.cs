using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LabAuthServer.Api.Extensions;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class SigningOverlapRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredOverlap_RejectsPreviousButPreservesActiveSigningAndValidation(bool certificateProvider)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var settings = new TokenOptions
        {
            Issuer = "https://issuer.example", Audience = "test", SigningAlgorithm = "RS256",
            ActiveKeyId = "active", PreviousKeyId = "previous",
            PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            ApprovedKeyIds = ["active", "previous"], SigningKeyStoreReference = "test://keys",
            MaximumClaimSize = 4096, MaximumTokenSize = 7680,
            AccessTokenLifetime = TimeSpan.FromHours(1)
        };
        if (certificateProvider)
        {
            settings.SigningCertificateStoreLocation = "LocalMachine";
            settings.SigningCertificateStoreName = "My";
            settings.SigningCertificateThumbprint = certificate.Thumbprint;
            settings.PreviousSigningCertificateThumbprint = new string('A', 40);
        }
        var options = Options.Create(settings);
        Assert.Empty(TokenOptionsValidator.Validate(settings)); // Startup is valid before the transition.
        IProtectedSigningKeyProvider provider = certificateProvider
            ? new CertificateSigningKeyProvider(options, (_, _) => certificate)
            : new ProtectedSigningKeyProvider(options, new Dictionary<string, RSA> { ["active"] = rsa, ["previous"] = rsa });
        using (var previous = await provider.GetValidationKeyAsync("previous")) Assert.Equal(2048, previous.PublicKey.KeySize);

        // Model the already-started process after overlap expiration without sleeping or changing a store.
        settings.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        Assert.NotEmpty(TokenOptionsValidator.Validate(settings)); // A new startup still rejects stale settings.
        using var active = await provider.GetActiveKeyAsync();
        using var validation = await provider.GetValidationKeyAsync("active");
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetValidationKeyAsync("previous").AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetKeyAsync("previous").AsTask());

        var signer = new RsaTokenSigningService(options, provider, NullLogger<RsaTokenSigningService>.Instance);
        var now = DateTimeOffset.UtcNow;
        var signed = await signer.SignAsync(new TokenClaims
        {
            Issuer = settings.Issuer, Audience = settings.Audience, Subject = "reader", Jti = "test",
            IssuedAt = now, ExpiresAt = now.AddMinutes(5), Roles = ["Reader"], Scopes = []
        });
        var bearer = new JwtBearerOptions();
        new JwtBearerAuthenticationOptions(options, provider).Configure(bearer);
        var result = await bearer.TokenHandlers.Single().ValidateTokenAsync(signed.AccessToken, bearer.TokenValidationParameters);
        Assert.True(result.IsValid, result.Exception?.GetType().Name);
    }
}
