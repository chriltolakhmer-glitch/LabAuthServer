using System.Security.Cryptography;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Verifies license signatures using RSA-PSS with SHA-256 (Phase 4 decision D-11).
/// The trusted key is selected by key identifier; an unknown key or unsupported algorithm
/// is reported as a structured failure, never as success. No private key is required.
/// </summary>
public sealed class RsaPssLicenseSignatureVerifier : ILicenseSignatureVerifier
{
    private readonly ITrustedLicenseKeyProvider _keyProvider;

    public RsaPssLicenseSignatureVerifier(ITrustedLicenseKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <inheritdoc />
    public LicenseSignatureVerificationOutcome Verify(
        byte[] signedPayload,
        string algorithm,
        string keyId,
        byte[] signature)
    {
        ArgumentNullException.ThrowIfNull(signedPayload);
        ArgumentNullException.ThrowIfNull(signature);

        if (!string.Equals(algorithm, LicenseConstants.RsaPssSha256Algorithm, StringComparison.Ordinal))
        {
            return LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.AlgorithmUnsupported);
        }

        if (string.IsNullOrEmpty(keyId))
        {
            return LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.KeyUntrusted);
        }

        var publicKey = _keyProvider.TryGetPublicKey(keyId);
        if (publicKey is null)
        {
            return LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.KeyUntrusted);
        }

        try
        {
            if (publicKey.KeySize < LicenseConstants.MinimumRsaKeySize)
            {
                // A trusted key below the minimum accepted size makes the key set unusable for
                // verification. The accepted algorithm is unchanged, so this is a configuration
                // failure rather than an algorithm failure (Phase 4.3).
                return LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.InvalidConfiguration);
            }

            var verified = publicKey.VerifyData(
                signedPayload,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return verified
                ? LicenseSignatureVerificationOutcome.Success()
                : LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.SignatureInvalid);
        }
        catch (CryptographicException)
        {
            return LicenseSignatureVerificationOutcome.Failed(LicenseValidationReason.SignatureInvalid);
        }
    }
}