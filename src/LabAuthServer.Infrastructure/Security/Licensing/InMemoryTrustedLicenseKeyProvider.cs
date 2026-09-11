using System.Security.Cryptography;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// In-memory trusted key set used to verify licenses. Keys are added, never replaced, so a
/// rotated key coexists with the key it supersedes. This type holds public keys only: a key
/// supplied to <see cref="Add"/> is copied from its public parameters alone, so any private
/// material the caller passes is discarded and never retained (Phase 4.3, finding F-3).
/// The provider owns its copies; callers keep ownership of the keys they supply.
/// </summary>
public sealed class InMemoryTrustedLicenseKeyProvider : ITrustedLicenseKeyProvider, IDisposable
{
    private readonly Dictionary<string, RSA> _publicKeys = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Adds a trusted public key. The key is copied from its public parameters only, so private
    /// material is never retained. Duplicate identifiers are rejected. The provider owns the
    /// stored copy; the caller retains ownership of <paramref name="publicKey"/>.
    /// </summary>
    public void Add(string keyId, RSA publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(publicKey);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var publicOnly = CreatePublicOnlyCopy(publicKey);

        if (!_publicKeys.TryAdd(keyId, publicOnly))
        {
            publicOnly.Dispose();
            throw new InvalidOperationException($"A trusted license key with identifier '{keyId}' already exists.");
        }
    }

    /// <inheritdoc />
    public RSA? TryGetPublicKey(string keyId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(keyId))
        {
            return null;
        }

        return _publicKeys.TryGetValue(keyId, out var key) ? key : null;
    }

    private static RSA CreatePublicOnlyCopy(RSA source)
    {
        var publicParameters = source.ExportParameters(includePrivateParameters: false);
        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(publicParameters);
        return publicOnly;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var key in _publicKeys.Values)
        {
            key.Dispose();
        }

        _publicKeys.Clear();
    }
}