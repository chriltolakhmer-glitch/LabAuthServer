using System.Security.Cryptography;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;
using LabAuthServer.Infrastructure.Security.Licensing;
using IssuerType = LabAuthServer.LicenseIssuer.LicenseIssuer;

namespace LabAuthServer.UnitTests.Licensing;

/// <summary>
/// Shared, reusable licensing test fixture (Phase 4.10, decision D4.10-5).
/// Every signing key is generated in memory at run time and disposed with the fixture; no key
/// material is read from disk, committed, or written to output. The fixture issues real licenses
/// through the Phase 4.4 issuer and builds real validators over the Phase 4.3/4.5 components so
/// tests exercise the production pipeline rather than a reimplementation of it.
/// </summary>
public sealed class LicenseTestFixture : IDisposable
{
    /// <summary>Stable, non-secret signing key identifier used by the fixture.</summary>
    public const string DefaultKeyId = "lab-license-signing-2026";

    /// <summary>Default issuance instant used by the fixture.</summary>
    public static readonly DateTimeOffset IssuedAt = new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Default expiry instant used by the fixture.</summary>
    public static readonly DateTimeOffset ExpiresAt = new(2027, 9, 11, 0, 0, 0, TimeSpan.Zero);

    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    /// <summary>Generates a fresh RSA-3072 signing key owned by this fixture.</summary>
    public RSA CreateKey(int bits = 3072)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = RSA.Create(bits);
        _disposables.Add(key);
        return key;
    }

    /// <summary>
    /// Builds a public-only copy of <paramref name="source"/> for use as a trusted verification
    /// key. The copy never contains private parameters.
    /// </summary>
    public RSA CreatePublicOnly(RSA source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(source.ExportParameters(includePrivateParameters: false));
        _disposables.Add(publicOnly);
        return publicOnly;
    }

    /// <summary>Issues a license and returns its container bytes.</summary>
    public byte[] Issue(
        RSA signingKey,
        string keyId = DefaultKeyId,
        LicenseEdition edition = LicenseEdition.Professional,
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyList<string>? features = null,
        IReadOnlyDictionary<string, int>? limits = null,
        string licenseId = "license-0001",
        string? customer = "Example Customer")
    {
        ArgumentNullException.ThrowIfNull(signingKey);

        var issuer = new IssuerType(new FixedSigningKeyProvider(keyId, signingKey));
        var result = issuer.Issue(new LabAuthServer.LicenseIssuer.LicenseIssuanceRequest
        {
            LicenseId = licenseId,
            Edition = edition,
            Customer = customer,
            IssuedAt = issuedAt ?? IssuedAt,
            ExpiresAt = expiresAt,
            Features = features ?? new[] { LicenseFeatureIds.AuthLdap },
            Limits = limits ?? new Dictionary<string, int> { [LicenseLimitKeys.MaximumUsers] = 100 },
            KeyId = keyId
        });

        Assert.True(result.Succeeded, $"test fixture issuance failed: {result.Reason}");
        return result.ContainerBytes!;
    }

    /// <summary>Builds a validator that trusts <paramref name="trustedKey"/> under <paramref name="keyId"/>.</summary>
    public LicenseValidator CreateValidator(
        RSA trustedKey,
        string keyId = DefaultKeyId,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(trustedKey);

        var provider = new InMemoryTrustedLicenseKeyProvider();
        provider.Add(keyId, trustedKey);
        _disposables.Add(provider);

        return new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new RsaPssLicenseSignatureVerifier(provider),
            new FixedClock(now ?? IssuedAt));
    }

    /// <summary>Builds a validator over several trusted keys (rotation scenario).</summary>
    public LicenseValidator CreateValidator(
        IEnumerable<KeyValuePair<string, RSA>> trustedKeys,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(trustedKeys);

        var provider = new InMemoryTrustedLicenseKeyProvider();
        foreach (var pair in trustedKeys)
        {
            provider.Add(pair.Key, pair.Value);
        }

        _disposables.Add(provider);

        return new LicenseValidator(
            new JsonLicenseDocumentParser(),
            new RsaPssLicenseSignatureVerifier(provider),
            new FixedClock(now ?? IssuedAt));
    }

    /// <summary>Builds a signing-key provider over an ephemeral key.</summary>
    public static LabAuthServer.LicenseIssuer.ILicenseSigningKeyProvider SigningKeyProvider(
        string keyId, RSA key)
        => new FixedSigningKeyProvider(keyId, key);

    /// <summary>Resolves the repository root by walking up to the solution file.</summary>
    public static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LabAuthServer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
    }

    /// <summary>Fixed UTC clock so every time-dependent assertion is deterministic (D4.10-6).</summary>
    public sealed class FixedClock : ILicenseClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    /// <summary>Signing-key provider over an ephemeral in-memory key.</summary>
    private sealed class FixedSigningKeyProvider : LabAuthServer.LicenseIssuer.ILicenseSigningKeyProvider
    {
        private readonly RSA _key;

        public FixedSigningKeyProvider(string keyId, RSA key)
        {
            KeyId = keyId;
            _key = key;
        }

        public string KeyId { get; }

        public RSA? TryGetSigningKey() => _key;
    }
}