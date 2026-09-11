using System.Security.Cryptography;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Resolves a trusted license-signing public key by key identifier.
/// The key set is additive so that key rotation does not invalidate licenses
/// signed by a previous key (Phase 4 decision D-12).
/// Only public keys are ever returned; no private key is available here.
/// </summary>
public interface ITrustedLicenseKeyProvider
{
    /// <summary>
    /// Returns the trusted public key for <paramref name="keyId"/>, or <c>null</c> when the
    /// identifier is not trusted. The caller owns the returned <see cref="RSA"/> instance.
    /// </summary>
    RSA? TryGetPublicKey(string keyId);
}