using System.Security.Cryptography;
using System.Text;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Phase 4.16 runtime license loader tests. Every key is ephemeral and in-memory; no private
/// key material is committed or written to output. Tests exercise the production
/// <see cref="LicensePolicyProvider"/> over the production reader, parser and verifier.
/// </summary>
public sealed class Phase416RuntimeLicenseLoaderTests : IDisposable
{
    private readonly LicenseTestFixture _fixture = new();
    private readonly string _tempDirectory;

    public Phase416RuntimeLicenseLoaderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "lab-license-416-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    private string WriteLicenseFile(byte[] content, string name = "license.lic")
    {
        var path = Path.Combine(_tempDirectory, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    private LicensePolicyProvider BuildProvider(
        string path,
        RSA trustedPublicKey,
        string trustedKeyId = LicenseTestFixture.DefaultKeyId,
        long maximumBytes = 64 * 1024,
        DateTimeOffset? now = null,
        CapturingLogger? logger = null)
    {
        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(trustedKeyId, trustedPublicKey);

        var validator = new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new RsaPssLicenseSignatureVerifier(provider),
            new LicenseTestFixture.FixedClock(now ?? LicenseTestFixture.IssuedAt));

        var options = Options.Create(new LicenseValidationOptions
        {
            LicenseFilePath = path,
            MaximumLicenseFileBytes = maximumBytes
        });

        return new LicensePolicyProvider(options, new BoundedLicenseFileReader(), validator, logger ?? new CapturingLogger());
    }

    [Fact]
    public void EmptyPath_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        var provider = BuildProvider(string.Empty, publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
        Assert.Equal(LicenseEdition.Community, provider.GetPolicy().Edition);
    }

    [Fact]
    public void MissingFile_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        var provider = BuildProvider(Path.Combine(_tempDirectory, "absent.lic"), publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void ValidLicense_LoadsNonRestrictedPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(_fixture.Issue(signingKey));

        var provider = BuildProvider(path, publicKey);

        var policy = provider.GetPolicy();
        Assert.False(policy.IsRestricted);
        Assert.Equal(LicenseEdition.Professional, policy.Edition);
        Assert.True(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
    }

    [Fact]
    public void MalformedFile_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(Encoding.UTF8.GetBytes("this is not a license"));

        var provider = BuildProvider(path, publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void InvalidSignature_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        var container = _fixture.Issue(signingKey);
        // Flip a byte inside the signature half of the container to break verification.
        var tampered = (byte[])container.Clone();
        tampered[^3] ^= 0xFF;
        var path = WriteLicenseFile(tampered);

        var provider = BuildProvider(path, publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void UnknownKey_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(_fixture.Issue(signingKey));

        // Trust the same key material under a different identifier so keyId lookup fails.
        var provider = BuildProvider(path, publicKey, trustedKeyId: "some-other-key");

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void ExpiredLicense_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        var issuedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expiresAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var path = WriteLicenseFile(_fixture.Issue(signingKey, issuedAt: issuedAt, expiresAt: expiresAt));

        var provider = BuildProvider(path, publicKey, now: new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.Zero));

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void UnsupportedVersion_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        // A well-formed container whose signed payload declares an unsupported format version.
        var payload = Encoding.UTF8.GetBytes("{\"licenseVersion\":2}");
        var container = "{\"payload\":\"" + Convert.ToBase64String(payload) +
            "\",\"algorithm\":\"" + LicenseConstants.RsaPssSha256Algorithm +
            "\",\"keyId\":\"" + LicenseTestFixture.DefaultKeyId +
            "\",\"signature\":\"AAAA\"}";
        var path = WriteLicenseFile(Encoding.UTF8.GetBytes(container));

        var provider = BuildProvider(path, publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void OversizedFile_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(new byte[2048]);

        var provider = BuildProvider(path, publicKey, maximumBytes: 1024);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void UnreadableFile_YieldsRestrictedCommunityPolicy()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(_fixture.Issue(signingKey));

        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        var provider = BuildProvider(path, publicKey);

        Assert.True(provider.GetPolicy().IsRestricted);
    }

    [Fact]
    public void RestrictedPolicy_DeniesAllFeaturesAndLimits()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);

        var provider = BuildProvider(string.Empty, publicKey);
        var policy = provider.GetPolicy();

        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthLdap));
        Assert.False(policy.IsFeatureEnabled(LicenseFeatureIds.AuthJwt));
        Assert.False(policy.TryGetLimit(LicenseLimitKeys.MaximumUsers, out _));
        Assert.False(policy.IsWithinLimit(LicenseLimitKeys.MaximumUsers, 0));
    }

    [Fact]
    public void ValidLicense_DoesNotEmitSensitiveMaterialToLogs()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var container = _fixture.Issue(signingKey);
        var path = WriteLicenseFile(container);

        var logger = new CapturingLogger();
        _ = BuildProvider(path, publicKey, logger: logger);

        var text = logger.Joined;
        Assert.DoesNotContain("payload", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signature", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRIVATE KEY", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Convert.ToBase64String(container), text, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidLicense_DoesNotEmitSensitiveMaterialToLogs()
    {
        using var signingKey = _fixture.CreateKey();
        using var publicKey = _fixture.CreatePublicOnly(signingKey);
        var path = WriteLicenseFile(Encoding.UTF8.GetBytes("not-a-license"));

        var logger = new CapturingLogger();
        _ = BuildProvider(path, publicKey, logger: logger);

        Assert.DoesNotContain("PRIVATE KEY", logger.Joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoundedReader_RejectsOversizedWithoutReadingWholeFile()
    {
        var path = WriteLicenseFile(new byte[4096]);
        var reader = new BoundedLicenseFileReader();

        var result = reader.Read(path, 1024);

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseFileReadStatus.TooLarge, result.Status);
    }

    [Fact]
    public void BoundedReader_ReturnsEmptyStatusForWhitespacePath()
    {
        var reader = new BoundedLicenseFileReader();

        var result = reader.Read("   ", 1024);

        Assert.Equal(LicenseFileReadStatus.PathEmpty, result.Status);
    }

    [Fact]
    public void TrustedKeySetFactory_AcceptsPublicKey_AndSkipsUnparsableEntry()
    {
        using var signingKey = _fixture.CreateKey();
        var publicPem = signingKey.ExportSubjectPublicKeyInfoPem();

        var provider = new InMemoryTrustedLicenseKeyProvider();
        TrustedKeySetFactory.Populate(provider, new[]
        {
            new TrustedLicenseKeyOptions { KeyId = "public-key", PublicKey = publicPem },
            new TrustedLicenseKeyOptions { KeyId = "not-a-key", PublicKey = "not a pem block" },
            new TrustedLicenseKeyOptions { KeyId = string.Empty, PublicKey = publicPem }
        });

        Assert.NotNull(provider.TryGetPublicKey("public-key"));
        Assert.Null(provider.TryGetPublicKey("not-a-key"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fixture.Dispose();

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of the temporary license directory.
        }
    }

    /// <summary>Minimal capturing logger for asserting on emitted licensing metadata.</summary>
    private sealed class CapturingLogger : ILogger<LicensePolicyProvider>
    {
        private readonly List<string> _messages = new();

        public string Joined => string.Join("\n", _messages);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }
}