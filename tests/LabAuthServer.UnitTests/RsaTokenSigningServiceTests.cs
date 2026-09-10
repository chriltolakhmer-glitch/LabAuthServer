using System.Security.Cryptography;
using System.Text.Json;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class RsaTokenSigningServiceTests
{
    [Fact]
    public async Task SignAsync_WithRuntimeRsaKey_ReturnsThreePartJwtWithConfiguredHeader()
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider);

        var result = await service.SignAsync(CreateClaims());

        var segments = result.AccessToken.Split('.');

        Assert.Equal(3, segments.Length);
        Assert.Equal("test-key-1", result.KeyIdentifier);
        Assert.DoesNotContain("PRIVATE KEY", result.AccessToken, StringComparison.OrdinalIgnoreCase);

        using var header = JsonDocument.Parse(DecodeBase64Url(segments[0]));
        Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());
        Assert.Equal("JWT", header.RootElement.GetProperty("typ").GetString());
        Assert.Equal("test-key-1", header.RootElement.GetProperty("kid").GetString());
    }

    [Fact]
    public async Task SignAsync_PreservesClaimsAndSignatureVerifiesWithCorrespondingPublicKey()
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider);

        var result = await service.SignAsync(CreateClaims());
        var segments = result.AccessToken.Split('.');
        var signature = Base64UrlDecode(segments[2]);
        var signingInput = System.Text.Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}");

        Assert.True(provider.Key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        using var payload = JsonDocument.Parse(DecodeBase64Url(segments[1]));
        Assert.Equal("https://issuer.example", payload.RootElement.GetProperty("iss").GetString());
        Assert.Equal("labauthserver-api", payload.RootElement.GetProperty("aud").GetString());
        Assert.Equal("user-123", payload.RootElement.GetProperty("sub").GetString());
        var roles = payload.RootElement.GetProperty("role");
        Assert.Equal(JsonValueKind.String, roles.ValueKind);
        Assert.Equal("Reader", roles.GetString());
        Assert.NotEmpty(payload.RootElement.GetProperty("jti").GetString()!);
        Assert.Equal(JsonValueKind.Number, payload.RootElement.GetProperty("iat").ValueKind);
        Assert.Equal(JsonValueKind.Number, payload.RootElement.GetProperty("nbf").ValueKind);
        Assert.Equal(JsonValueKind.Number, payload.RootElement.GetProperty("exp").ValueKind);
    }

    [Theory]
    [InlineData("HS256")]
    [InlineData("none")]
    public async Task SignAsync_WithUnsupportedAlgorithm_Throws(string algorithm)
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider, algorithm);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SignAsync(CreateClaims()));

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignAsync_WhenProviderKeyIdentifierDiffers_FailsClosed()
    {
        using var provider = new RuntimeSigningKeyProvider("different-key");
        var service = CreateService(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SignAsync(CreateClaims()));

        Assert.Contains("key identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignAsync_WhenProviderHasNoKey_FailsClosedWithoutPrivateKeyDetails()
    {
        var provider = new MissingSigningKeyProvider();
        var service = CreateService(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SignAsync(CreateClaims()));

        Assert.DoesNotContain("PRIVATE KEY", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RSA", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignAsync_WhenCancelledBeforeKeyAccess_DoesNotAccessProvider()
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SignAsync(CreateClaims(), cancellationTokenSource.Token));
        Assert.False(provider.WasAccessed);
    }

    [Fact]
    public async Task SignAsync_WithInvalidConfiguration_FailsClosed()
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider, signingAlgorithm: "RS256", audience: "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SignAsync(CreateClaims()));

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(provider.WasAccessed);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("subject")]
    public async Task SignAsync_PreservesUnicodeCharacterLimits(string field)
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider, maximumClaimSize: 64);
        var value = field == "issuer" ? "https://" + new string('\u00e9', 56) : new string('\u00e9', 64);
        var claims = field switch
        {
            "issuer" => CreateClaims() with { Issuer = value },
            "audience" => CreateClaims() with { Audience = value },
            _ => CreateClaims() with { Subject = value }
        };
        var signed = await service.SignAsync(claims);
        using var payload = JsonDocument.Parse(DecodeBase64Url(signed.AccessToken.Split('.')[1]));
        var claimName = field switch { "issuer" => "iss", "audience" => "aud", _ => "sub" };
        Assert.Equal(value, payload.RootElement.GetProperty(claimName).GetString());
        var excessive = field switch
        {
            "issuer" => claims with { Issuer = value + "x" },
            "audience" => claims with { Audience = value + "x" },
            _ => claims with { Subject = value + "x" }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignAsync(excessive));
    }

    [Fact]
    public async Task SignAsync_PreservesExactSerializedPayloadBudget()
    {
        using var provider = new RuntimeSigningKeyProvider("test-key-1");
        var service = CreateService(provider);
        var input = CreateClaims() with { Scopes = [new string('a', 4096), "b"] };
        var initial = await service.SignAsync(input);
        var size = Base64UrlDecode(initial.AccessToken.Split('.')[1]).Length;
        var lastScope = new string('b', 1 + 7680 - size);
        var maximal = input with { Scopes = [new string('a', 4096), lastScope] };
        var signed = await service.SignAsync(maximal);
        Assert.Equal(7680, Base64UrlDecode(signed.AccessToken.Split('.')[1]).Length);
        Assert.InRange(signed.AccessToken.Length, 7681, 12288);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignAsync(maximal with
        {
            Scopes = [new string('a', 4096), lastScope + "x"]
        }));
    }

    private static RsaTokenSigningService CreateService(
        IProtectedSigningKeyProvider provider,
        string signingAlgorithm = "RS256",
        string audience = "labauthserver-api",
        int maximumClaimSize = 4096)
    {
        var options = Options.Create(new TokenOptions
        {
            Issuer = "https://issuer.example",
            Audience = audience,
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            ClockSkew = TimeSpan.FromMinutes(5),
            SigningAlgorithm = signingAlgorithm,
            ActiveKeyId = "test-key-1",
            SigningKeyStoreReference = "runtime-test-only",
            MaximumClaimSize = maximumClaimSize,
            MaximumTokenSize = 7680
        });

        return new RsaTokenSigningService(
            options,
            provider,
            NullLogger<RsaTokenSigningService>.Instance);
    }

    private static TokenClaims CreateClaims()
    {
        return new TokenClaims
        {
            Issuer = "https://issuer.example",
            Audience = "labauthserver-api",
            Subject = "user-123",
            Jti = "test-jti",
            IssuedAt = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero),
            ExpiresAt = new DateTimeOffset(2026, 8, 31, 12, 15, 0, TimeSpan.Zero),
            Roles = ["Reader"],
            Scopes = ["reports.read"]
        };
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private static string DecodeBase64Url(string value)
    {
        return System.Text.Encoding.UTF8.GetString(Base64UrlDecode(value));
    }

    private sealed class RuntimeSigningKeyProvider : IProtectedSigningKeyProvider, IDisposable
    {
        public RuntimeSigningKeyProvider(string keyIdentifier)
        {
            KeyIdentifier = keyIdentifier;
            Key = RSA.Create(2048);
        }

        public string KeyIdentifier { get; }

        public RSA Key { get; }

        public bool WasAccessed { get; private set; }

        public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default)
        {
            WasAccessed = true;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new SigningKeyMaterial(KeyIdentifier, RSA.Create(Key.ExportParameters(true))));
        }

        public void Dispose()
        {
            Key.Dispose();
        }
    }

    private sealed class MissingSigningKeyProvider : IProtectedSigningKeyProvider
    {
        public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("No signing key is available.");
        }
    }
}
