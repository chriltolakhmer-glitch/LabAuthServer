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

    ValueTask<ValidationKeyMaterial> GetValidationKeyAsync(
        string keyIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new ArgumentException("A signing key identifier is required.", nameof(keyIdentifier));
        }

        throw new NotSupportedException("This key provider does not expose validation keys.");
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

public sealed class ValidationKeyMaterial : IDisposable
{
    private bool _disposed;

    public ValidationKeyMaterial(string keyIdentifier, RSA publicKey)
    {
        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new ArgumentException("Signing key identifier is required.", nameof(keyIdentifier));
        }

        PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        KeyIdentifier = keyIdentifier;
    }

    public string KeyIdentifier { get; }

    public RSA PublicKey { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PublicKey.Dispose();
        _disposed = true;
    }
}
