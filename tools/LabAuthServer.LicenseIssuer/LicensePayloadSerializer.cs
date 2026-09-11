using System.Globalization;
using System.Text.Json;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Deterministic serializer for the signed license payload (Phase 4 decision O-03, current
/// implementation). Emits a fixed property order, camelCase names, no whitespace, UTF-8 without a
/// BOM, and UTC timestamps at second precision with a <c>Z</c> suffix. Limits are written in
/// ordinal key order. The returned bytes are the exact sequence that is signed and that the server
/// verifier validates against the embedded payload.
/// This profile is the current issuer implementation, not a permanent canonicalization standard;
/// O-03 remains open for a future license format version.
/// </summary>
public static class LicensePayloadSerializer
{
    /// <summary>Serializes <paramref name="document"/> to its canonical payload bytes.</summary>
    public static byte[] Serialize(LicenseDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("licenseVersion", document.LicenseVersion);
            writer.WriteString("licenseId", document.LicenseId);
            writer.WriteString("product", document.Product);
            writer.WriteString("edition", document.Edition.ToString());
            WriteNullableString(writer, "customer", document.Customer);
            writer.WriteString("issuedAt", FormatUtc(document.IssuedAt));

            if (document.ExpiresAt is { } expiresAt)
            {
                writer.WriteString("expiresAt", FormatUtc(expiresAt));
            }
            else
            {
                writer.WriteNull("expiresAt");
            }

            writer.WriteStartArray("features");
            foreach (var feature in document.Features)
            {
                writer.WriteStringValue(feature);
            }

            writer.WriteEndArray();

            writer.WriteStartObject("limits");
            foreach (var limit in document.Limits.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteNumber(limit.Key, limit.Value);
            }

            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static string FormatUtc(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}