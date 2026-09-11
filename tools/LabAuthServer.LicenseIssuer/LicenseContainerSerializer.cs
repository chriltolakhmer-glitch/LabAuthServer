using System.Text.Json;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Serializes a signed license into the container format read by the server's
/// <c>JsonLicenseDocumentParser</c>: a single JSON object carrying the Base64 signed payload and
/// the signature envelope (algorithm, keyId, signature). The container carries no private key
/// material. As recorded under finding F-2, the envelope fields are selection metadata outside the
/// signed payload and are not claimed to be cryptographically authenticated.
/// </summary>
public static class LicenseContainerSerializer
{
    /// <summary>Serializes <paramref name="license"/> to container bytes.</summary>
    public static byte[] Serialize(SignedLicense license)
    {
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(license.Signature);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("payload", Convert.ToBase64String(license.SignedPayload));
            writer.WriteString("algorithm", license.Signature.Algorithm);
            writer.WriteString("keyId", license.Signature.KeyId);
            writer.WriteString("signature", license.Signature.Signature);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}