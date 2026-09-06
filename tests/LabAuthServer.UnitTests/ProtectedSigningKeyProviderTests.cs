using System.Security.Cryptography;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class ProtectedSigningKeyProviderTests
{
    [Fact]
    public async Task GetActiveKeyAsync_WithConfiguredActiveKey_ReturnsActiveKey()
    {
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = CreateProvider(activeKey, previousKey, activeKeyId: "active-key-1");

        using var activeSigningKey = await provider.GetActiveKeyAsync();

        Assert.Equal("active-key-1", activeSigningKey.KeyIdentifier);
        Assert.NotNull(activeSigningKey.PrivateKey);
    }

    [Fact]
    public async Task GetActiveKeyAsync_WithPreviousKeyApprovedWithinOverlapWindow_ReturnsPreviousKeyWhenRequested()
    {
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = CreateProvider(activeKey, previousKey, activeKeyId: "active-key-1", previousKeyId: "previous-key-1");

        using var overlapKey = await provider.GetKeyAsync("previous-key-1");

        Assert.Equal("previous-key-1", overlapKey.KeyIdentifier);
    }

    [Fact]
    public async Task GetKeyAsync_WithUnknownKeyIdentifier_FailsClosed()
    {
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = CreateProvider(activeKey, previousKey, activeKeyId: "active-key-1", previousKeyId: "previous-key-1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("ghost-key").AsTask());

        Assert.Contains("not approved", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetActiveKeyAsync_WithMissingActiveKey_ThrowsWithoutLeakingKeyMaterial()
    {
        using var previousKey = RSA.Create(2048);
        var options = Options.Create(new TokenOptions
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
        });

        var provider = new ProtectedSigningKeyProvider(options, new[]
        {
            new KeyValuePair<string, RSA>("previous-key-1", previousKey)
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetActiveKeyAsync().AsTask());

        Assert.Contains("active signing key", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRIVATE KEY", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RSA", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_WithExpiredOverlapWindow_FailsClosed()
    {
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = CreateProvider(
            activeKey,
            previousKey,
            activeKeyId: "active-key-1",
            previousKeyId: "previous-key-1",
            previousKeyExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            approvedKeyIds: ["active-key-1", "previous-key-1"]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("previous-key-1").AsTask());

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetActiveKeyAsync_WhenKeyIdentifiersAreDuplicated_FailsClosed()
    {
        using var activeKey = RSA.Create(2048);
        using var previousKey = RSA.Create(2048);
        var provider = CreateProvider(activeKey, previousKey, activeKeyId: "active-key-1", approvedKeyIds: ["active-key-1", "active-key-1"]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetActiveKeyAsync().AsTask());

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ProtectedSigningKeyProvider CreateProvider(
        RSA? activeKey,
        RSA? previousKey,
        string activeKeyId,
        string? previousKeyId = null,
        DateTimeOffset? previousKeyExpiresAt = null,
        IReadOnlyList<string>? approvedKeyIds = null)
    {
        var active = activeKey ?? RSA.Create(2048);
        var previous = previousKey ?? RSA.Create(2048);
        var options = Options.Create(new TokenOptions
        {
            Issuer = "https://issuer.example",
            Audience = "labauthserver-api",
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            ClockSkew = TimeSpan.FromMinutes(5),
            SigningAlgorithm = "RS256",
            ActiveKeyId = activeKeyId,
            PreviousKeyId = previousKeyId ?? string.Empty,
            PreviousKeyExpiresAt = previousKeyExpiresAt ?? DateTimeOffset.UtcNow.AddDays(1),
            ApprovedKeyIds = approvedKeyIds ?? [activeKeyId, previousKeyId ?? "another-key"],
            SigningKeyStoreReference = "environment://LabAuthServer/SigningKey",
            MaximumClaimSize = 4096,
            MaximumTokenSize = 16384
        });

        return new ProtectedSigningKeyProvider(options, new[]
        {
            new KeyValuePair<string, RSA>(activeKeyId, active),
            new KeyValuePair<string, RSA>(previousKeyId ?? "another-key", previous)
        });
    }
}
