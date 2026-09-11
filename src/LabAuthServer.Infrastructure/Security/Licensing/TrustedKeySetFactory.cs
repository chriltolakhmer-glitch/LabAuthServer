using System.Security.Cryptography;
using LabAuthServer.Application.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Builds the trusted public-key set from configuration (Phase 4.16). Only PEM-encoded
/// public keys are accepted; private-key PEM blocks are rejected so no private material
/// can be provisioned into the server. Invalid entries are skipped rather than throwing,
/// so a misconfigured key cannot crash startup.
/// </summary>
public static class TrustedKeySetFactory
{
    private const string PrivateKeyMarker = "PRIVATE KEY";

    /// <summary>Populates <paramref name="provider"/> from the configured trusted keys.</summary>
    public static void Populate(
        InMemoryTrustedLicenseKeyProvider provider,
        IEnumerable<TrustedLicenseKeyOptions> keys)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var entry in keys)
        {
            if (string.IsNullOrWhiteSpace(entry.KeyId) || string.IsNullOrWhiteSpace(entry.PublicKey))
            {
                continue;
            }

            // Reject any PEM block that carries private material outright.
            if (entry.PublicKey.Contains(PrivateKeyMarker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(entry.PublicKey.AsSpan());
                provider.Add(entry.KeyId, rsa);
                rsa.Dispose();
            }
            catch (CryptographicException)
            {
                // Skip an unparsable key; fail closed by simply not trusting it.
            }
            catch (ArgumentException)
            {
                // Skip an invalid key identifier; fail closed.
            }
        }
    }
}