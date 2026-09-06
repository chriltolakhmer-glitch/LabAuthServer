using LabAuthServer.Infrastructure.Security;

namespace LabAuthServer.UnitTests;

public sealed class TokenOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_ReturnsNoFailures()
    {
        var failures = TokenOptionsValidator.Validate(CreateValidOptions());

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_WithMissingSecurityValues_ReturnsFailures()
    {
        var failures = TokenOptionsValidator.Validate(new TokenOptions());

        Assert.NotEmpty(failures);
        Assert.Contains(failures, failure => failure.Contains("issuer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("audience", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("algorithm", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("key", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("http://issuer.example")]
    [InlineData("issuer.example")]
    public void Validate_WithNonHttpsIssuer_ReturnsFailure(string issuer)
    {
        var options = CreateValidOptions();
        options.Issuer = issuer;

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("HTTPS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithNonRsaAlgorithm_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.SigningAlgorithm = "HS256";

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("RSA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithInvalidLifetimeOrSizes_ReturnsFailures()
    {
        var options = CreateValidOptions();
        options.AccessTokenLifetime = TimeSpan.Zero;
        options.MaximumClaimSize = 4096;
        options.MaximumTokenSize = 1024;

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("lifetime", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("token size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithKeyMaterialInReference_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.SigningKeyStoreReference = "-----BEGIN PRIVATE KEY-----";

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("key store", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithExpiredPreviousKey_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.PreviousKeyId = "previous-key-1";
        options.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        options.ApprovedKeyIds = ["development-key-1", "previous-key-1"];

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("overlap expiry", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithDuplicateApprovedKeys_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.ApprovedKeyIds = ["development-key-1", "development-key-1"];

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Contains(failures, failure => failure.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithApprovedExternalKeyStoreAndRotationMetadata_ReturnsNoFailures()
    {
        var options = CreateValidOptions();
        options.SigningKeyStoreReference = "environment://LabAuthServer/SigningKey";
        options.ActiveKeyId = "active-key-1";
        options.PreviousKeyId = "previous-key-1";
        options.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        options.ApprovedKeyIds = ["active-key-1", "previous-key-1"];

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_WithCertificateStoreConfiguration_ReturnsNoFailures()
    {
        var options = CreateValidOptions();
        options.SigningCertificateStoreLocation = "LocalMachine";
        options.SigningCertificateStoreName = "My";
        options.SigningCertificateThumbprint = " BD5 45BA 289E BFC6 45C8 C3DC 4243 1197 5579D7E09 ";
        options.SigningKeyStoreReference = "Cert:\\LocalMachine\\My\\BD545BA289EBFC645C8C3DC424311975579D7E09";

        var failures = TokenOptionsValidator.Validate(options);

        Assert.Empty(failures);
    }

    private static TokenOptions CreateValidOptions()
    {
        return new TokenOptions
        {
            Issuer = "https://issuer.example",
            Audience = "labauthserver-api",
            AccessTokenLifetime = TimeSpan.FromHours(1),
            ClockSkew = TimeSpan.FromMinutes(5),
            SigningAlgorithm = "RS256",
            ActiveKeyId = "development-key-1",
            PreviousKeyId = "previous-key-1",
            PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            ApprovedKeyIds = ["development-key-1", "previous-key-1"],
            SigningCertificateStoreLocation = "LocalMachine",
            SigningCertificateStoreName = "My",
            SigningCertificateThumbprint = "BD545BA289EBFC645C8C3DC424311975579D7E09",
            PreviousSigningCertificateThumbprint = "AA545BA289EBFC645C8C3DC424311975579D7E09",
            SigningKeyStoreReference = "Cert:\\LocalMachine\\My\\BD545BA289EBFC645C8C3DC424311975579D7E09",
            MaximumClaimSize = 4096,
            MaximumTokenSize = 16384
        };
    }
}
