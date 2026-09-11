using System.Security.Cryptography;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// In-memory trusted key set used to verify licenses. Keys are added, never replaced, so a
/// rotated key coexists with the key it supersedes. This type holds public keys only.
/// </summary>
public sealed class InMemoryTrustedLicenseKeyProvider : ITrustedLicenseKeyProvider, IDisposable
{
    private readonly Dictionary<string, RSA> _publicKeys = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>Adds a trusted public key. Duplicate identifiers are rejected.</summary>
    public void Add(string keyId, RSA publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(publicKey);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (publicKey is RSACryptoServiceProvider)
        {
            throw new ArgumentException("The trusted license key must be a public-only RSA key.", nameof(publicKey));
        }

        if (!_publicKeys.TryAdd(keyId, publicKey))
        {
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