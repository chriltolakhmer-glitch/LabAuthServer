using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Services;

namespace LabAuthServer.UnitTests;

public sealed class TokenClaimsBuilderTests
{
    [Fact]
    public void Build_ConstructsRequiredRegisteredClaims()
    {
        var issuedAt = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var builder = CreateBuilder();

        var claims = builder.Build(
            new TokenIssuanceRequest { Subject = "user-123", Roles = ["Reader"] },
            issuedAt);

        Assert.Equal("https://issuer.example", claims.Issuer);
        Assert.Equal("labauthserver-api", claims.Audience);
        Assert.Equal("user-123", claims.Subject);
        Assert.NotEmpty(claims.Jti);
        Assert.Equal(issuedAt, claims.IssuedAt);
        Assert.Equal(issuedAt.AddMinutes(15), claims.ExpiresAt);
        Assert.Null(claims.NotBefore);
    }

    [Fact]
    public void Build_MapsAndNormalizesApprovedAuthorizationClaims()
    {
        var builder = CreateBuilder();

        var claims = builder.Build(
            new TokenIssuanceRequest
            {
                Subject = "user-123",
                Roles = [" Operator ", " Operator "],
                Scopes = ["reports.read", "reports.write"]
            },
            DateTimeOffset.UtcNow);

        Assert.Equal(["Operator"], claims.Roles);
        Assert.Equal(["reports.read", "reports.write"], claims.Scopes);
    }

    [Fact]
    public void Build_DoesNotEmitUnapprovedOrSensitiveClaims()
    {
        var claims = CreateBuilder().Build(
            new TokenIssuanceRequest
            {
                Subject = "user-123",
                Roles = ["Reader"],
                Scopes = ["reports.read"]
            },
            DateTimeOffset.UtcNow);

        var propertyNames = typeof(TokenClaims).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("Password", propertyNames);
        Assert.DoesNotContain("RawLdapResponse", propertyNames);
        Assert.DoesNotContain("DirectoryAttributes", propertyNames);
        Assert.DoesNotContain("Groups", propertyNames);
        Assert.DoesNotContain("UserDistinguishedName", propertyNames);
        Assert.Equal(9, propertyNames.Length);
        Assert.NotNull(claims.Roles);
        Assert.NotNull(claims.Scopes);
    }

    [Fact]
    public void Build_RejectsMissingSubjectSafely()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateBuilder().Build(
            new TokenIssuanceRequest { Subject = " " },
            DateTimeOffset.UtcNow));

        Assert.DoesNotContain("user-", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_RejectsClaimExceedingConfiguredSize()
    {
        var builder = new TokenClaimsBuilder(
            "https://issuer.example",
            "labauthserver-api",
            TimeSpan.FromMinutes(15),
            maximumClaimSize: 4,
            maximumTokenSize: 1024);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build(
            new TokenIssuanceRequest
            {
                Subject = "user-123",
                Roles = ["Reader"]
            },
            DateTimeOffset.UtcNow));

        Assert.Contains("claim", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_RejectsAggregateTokenSizeLimit()
    {
        var builder = new TokenClaimsBuilder(
            "https://issuer.example",
            "labauthserver-api",
            TimeSpan.FromMinutes(15),
            maximumClaimSize: 20,
            maximumTokenSize: 20);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build(
            new TokenIssuanceRequest { Subject = "user-123", Roles = ["Reader"] },
            DateTimeOffset.UtcNow));

        Assert.Contains("token size", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_IsDeterministicForSameApprovedInput()
    {
        var issuedAt = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var request = new TokenIssuanceRequest
        {
            Subject = "user-123",
            Roles = ["Reader"],
            Scopes = ["reports.write", "reports.read"]
        };
        var builder = CreateBuilder();

        var first = builder.Build(request, issuedAt);
        var second = builder.Build(request, issuedAt);

        Assert.Equal(first.Issuer, second.Issuer);
        Assert.Equal(first.Audience, second.Audience);
        Assert.Equal(first.Subject, second.Subject);
        Assert.Equal(first.IssuedAt, second.IssuedAt);
        Assert.Equal(first.ExpiresAt, second.ExpiresAt);
        Assert.NotEqual(first.Jti, second.Jti);
        Assert.Equal(first.Roles, second.Roles);
        Assert.Equal(first.Scopes, second.Scopes);
    }

    [Fact]
    public void Constructor_RejectsInvalidRequiredConfiguration()
    {
        Assert.Throws<ArgumentException>(() => CreateBuilder(issuer: "http://issuer.example"));
        Assert.Throws<ArgumentException>(() => CreateBuilder(audience: "api audience"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBuilder(lifetime: TimeSpan.Zero));
    }

    [Theory]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public void Build_PreservesUtf8ScopeLimit(int characters, bool accepted)
    {
        var builder = new TokenClaimsBuilder("https://issuer.example", "api", TimeSpan.FromMinutes(1), 64, 16384);
        var input = new TokenIssuanceRequest { Subject = "reader", Roles = ["Reader"], Scopes = [new string('\u00e9', characters)] };
        if (accepted)
            Assert.Equal(input.Scopes, builder.Build(input, DateTimeOffset.UtcNow).Scopes);
        else
            Assert.Throws<ArgumentException>(() => builder.Build(input, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void Build_PreservesApprovedRoleByteLimit(int maximumClaimSize, bool accepted)
    {
        var builder = new TokenClaimsBuilder("https://issuer.example", "api", TimeSpan.FromMinutes(1), maximumClaimSize, 16384);
        var input = new TokenIssuanceRequest { Subject = "reader", Roles = ["Reader"] };
        if (accepted)
            Assert.Equal(input.Roles, builder.Build(input, DateTimeOffset.UtcNow).Roles);
        else
            Assert.Throws<ArgumentException>(() => builder.Build(input, DateTimeOffset.UtcNow));
    }

    private static TokenClaimsBuilder CreateBuilder(
        string issuer = "https://issuer.example",
        string audience = "labauthserver-api",
        TimeSpan? lifetime = null)
    {
        return new TokenClaimsBuilder(
            issuer,
            audience,
            lifetime ?? TimeSpan.FromMinutes(15),
            maximumClaimSize: 4096,
            maximumTokenSize: 16384);
    }
}
