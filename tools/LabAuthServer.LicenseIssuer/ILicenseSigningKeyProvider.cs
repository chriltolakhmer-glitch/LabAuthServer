using System.Security.Cryptography;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Supplies the vendor-side RSA private signing key used to sign a license.
/// This is the only issuer abstraction that touches private key material.
/// Production key sources (certificate store, HSM, vault) are deferred (Phase 4 decision O-07);
/// tests supply an ephemeral in-memory key. Implementations must never log, serialize, or
/// otherwise expose private key material and must fail closed by returning <c>null</c> when no
/// usable key is available.
/// </summary>
public interface ILicenseSigningKeyProvider
{
    /// <summary>
    /// Identifier of the signing key recorded in the license envelope and configured in the
    /// server's trusted public-key set. Must be a stable, non-secret ASCII string.
    /// </summary>
    string KeyId { get; }

    /// <summary>
    /// Returns the RSA signing key, or <c>null</c> when no usable key is available.
    /// The caller does not own the returned instance and must not dispose it.
    /// </summary>
    RSA? TryGetSigningKey();
}