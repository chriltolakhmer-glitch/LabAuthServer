using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

public sealed class LicenseDocumentModelTests
{
    [Fact]
    public void ValidLicense_CanBeRepresented()
    {
        var document = CreateValidDocument();

        Assert.Equal(1, document.LicenseVersion);
        Assert.Equal("license-0001", document.LicenseId);
        Assert.Equal(LicenseConstants.Product, document.Product);
        Assert.Equal(LicenseEdition.Professional, document.Edition);
        Assert.Equal("Example Customer", document.Customer);
        Assert.Contains("auth.ldap", document.Features);
        Assert.Equal(100, document.Limits["max.users"]);
    }

    [Fact]
    public void PerpetualLicense_SupportsNullExpiration()
    {
        var document = CreateValidDocument() with { ExpiresAt = null };

        Assert.Null(document.ExpiresAt);
        Assert.True(document.IsPerpetual);
    }

    [Fact]
    public void TimeLimitedLicense_SupportsUtcExpiration()
    {
        var expiry = new DateTimeOffset(2027, 9, 11, 0, 0, 0, TimeSpan.Zero);
        var document = CreateValidDocument() with { ExpiresAt = expiry };

        Assert.Equal(expiry, document.ExpiresAt);
        Assert.Equal(TimeSpan.Zero, document.ExpiresAt!.Value.Offset);
        Assert.False(document.IsPerpetual);
    }

    [Fact]
    public void FeatureIdentifiers_AreRepresentedCorrectly()
    {
        var document = CreateValidDocument() with
        {
            Features = new[] { "auth.jwt", "audit.logging" }
        };

        Assert.Equal(2, document.Features.Count);
        Assert.Contains("auth.jwt", document.Features);
        Assert.Contains("audit.logging", document.Features);
    }

    [Fact]
    public void Limits_AreRepresentedCorrectly()
    {
        var document = CreateValidDocument() with
        {
            Limits = new Dictionary<string, int> { ["max.users"] = 250 }
        };

        Assert.Equal(250, document.Limits["max.users"]);
    }

    private static LicenseDocument CreateValidDocument() => new()
    {
        LicenseVersion = 1,
        LicenseId = "license-0001",
        Product = LicenseConstants.Product,
        Edition = LicenseEdition.Professional,
        Customer = "Example Customer",
        IssuedAt = new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero),
        ExpiresAt = new DateTimeOffset(2027, 9, 11, 0, 0, 0, TimeSpan.Zero),
        Features = new[] { "auth.ldap" },
        Limits = new Dictionary<string, int> { ["max.users"] = 100 }
    };
}