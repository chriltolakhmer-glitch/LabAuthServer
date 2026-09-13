using System.Security.Cryptography;
using LabAuthServer.Application.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Builds the trusted public-key set from configuration (Phase 4.16). Only PEM-encoded
/// public keys are accepted; private-key PEM blocks are rejected so no private material
/// can be provisioned into the server. Invalid entries are skipped and reported as an
/// invalid key set rather than throwing, so a misconfigured key cannot grant a license.
/// </summary>
public static class TrustedKeySetFactory
{
    private const string PrivateKeyMarker = "PRIVATE KEY";

    /// <summary>Populates <paramref name="provider"/> from the configured trusted keys.</summary>
    public static bool Populate(
        InMemoryTrustedLicenseKeyProvider provider,
        IEnumerable<TrustedLicenseKeyOptions> keys)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(keys);
        var configurationValid = true;

        foreach (var entry in keys)
        {
            if (string.IsNullOrWhiteSpace(entry.KeyId) || string.IsNullOrWhiteSpace(entry.PublicKey))
            {
                configurationValid = false;
                continue;
            }

            // Reject any PEM block that carries private material outright.
            if (entry.PublicKey.Contains(PrivateKeyMarker, StringComparison.OrdinalIgnoreCase))
            {
                configurationValid = false;
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
                configurationValid = false;
            }
            catch (ArgumentException)
            {
                configurationValid = false;
            }
            catch (InvalidOperationException)
            {
                configurationValid = false;
            }
        }

        return configurationValid;
    }
}