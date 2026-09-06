using System.Security.Cryptography;

namespace LabAuthServer.Infrastructure.Security;

public interface IProtectedSigningKeyProvider
{
    ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default);

    ValueTask<SigningKeyMaterial> GetKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new ArgumentException("A signing key identifier is required.", nameof(keyIdentifier));
        }

        return GetActiveKeyAsync(cancellationToken);
    }
}

public sealed class SigningKeyMaterial : IDisposable
{
    private bool _disposed;

    public SigningKeyMaterial(string keyIdentifier, RSA privateKey)
    {
        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new ArgumentException("Signing key identifier is required.", nameof(keyIdentifier));
        }

        PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
        KeyIdentifier = keyIdentifier;
    }

    public string KeyIdentifier { get; }

    public RSA PrivateKey { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PrivateKey.Dispose();
        _disposed = true;
    }
}
