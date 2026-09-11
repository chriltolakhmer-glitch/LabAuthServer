using System.Security.Cryptography;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Signs license payloads with RSA-PSS and SHA-256 (Phase 4 decision D-11) and returns the raw
/// signature bytes. Base64 encoding is applied by the issuer. No other padding or hash is used:
/// PKCS#1 v1.5, SHA-1, ECDSA and symmetric algorithms are never selected.
/// </summary>
public sealed class RsaPssLicenseSigner : ILicenseSigner
{
    /// <inheritdoc />
    public byte[] Sign(byte[] payload, RSA key)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(key);

        if (key.KeySize < LicenseConstants.MinimumRsaKeySize)
        {
            throw new InvalidOperationException("The RSA signing key is below the minimum accepted size.");
        }

        return key.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }
}