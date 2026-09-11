using System.Globalization;
using System.Text;
using System.Text.Json;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Strict parser for the license container format.
/// The container is a JSON object carrying the verbatim signed payload as base64 plus a
/// signature envelope. Canonicalization therefore remains an issuer-side concern and is not
/// fixed by this parser (Phase 4 format decision remains TO BE CONFIRMED DURING IMPLEMENTATION).
/// Ambiguous input (duplicate keys, BOM, trailing data, wrong casing, float limits) is rejected.
/// </summary>
public sealed class JsonLicenseDocumentParser : ILicenseDocumentParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
        MaxDepth = 16
    };

    // Exact property sets defined by the license format (Phase 4.5, finding M-1).
    // Any other property name is rejected rather than silently ignored (default-deny).
    private static readonly string[] AllowedContainerProperties =
    {
        "payload", "algorithm", "keyId", "signature"
    };

    private static readonly string[] AllowedPayloadProperties =
    {
        "licenseVersion", "licenseId", "product", "edition", "customer",
        "issuedAt", "expiresAt", "features", "limits"
    };

    /// <inheritdoc />
    public LicenseParseOutcome Parse(ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        // Reject a UTF-8 BOM; the signed bytes must not carry it.
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        string text;
        try
        {
            text = StrictUtf8.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        using var container = ParseDocument(text);
        if (container is null)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        var root = container.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        if (!HasUniqueProperties(root))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        if (!HasOnlyAllowedProperties(root, AllowedContainerProperties))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.FieldUnknown);
        }

        if (!TryGetRequiredString(root, "payload", out var payloadBase64))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.FieldMissing);
        }

        if (!TryGetRequiredString(root, "algorithm", out var algorithm) ||
            !TryGetRequiredString(root, "keyId", out var keyId) ||
            !TryGetRequiredString(root, "signature", out var signatureBase64))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.SignatureMalformed);
        }

        // The signature field carries Base64 text (Phase 4.3 decision D4.3-4). Convert ignores
        // whitespace, so reject it explicitly and require a non-empty decoding; a malformed or
        // non-canonical encoding must not reach the verifier.
        try
        {
            if (signatureBase64.Any(char.IsWhiteSpace) ||
                Convert.FromBase64String(signatureBase64).Length == 0)
            {
                return LicenseParseOutcome.Failed(LicenseValidationReason.SignatureMalformed);
            }
        }
        catch (FormatException)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.SignatureMalformed);
        }

        // The payload field carries Base64 text; apply the same strict rule as the signature
        // (Phase 4.5, finding M-2): reject whitespace, reject an empty decoding, and reject
        // malformed Base64 rather than normalising it before verification.
        byte[] payloadBytes;
        try
        {
            if (payloadBase64.Any(char.IsWhiteSpace))
            {
                return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
            }

            payloadBytes = Convert.FromBase64String(payloadBase64);
            if (payloadBytes.Length == 0)
            {
                return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
            }
        }
        catch (FormatException)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        string payloadText;
        try
        {
            payloadText = StrictUtf8.GetString(payloadBytes);
        }
        catch (DecoderFallbackException)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        using var payloadDocument = ParseDocument(payloadText);
        if (payloadDocument is null)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        var payloadRoot = payloadDocument.RootElement;
        if (payloadRoot.ValueKind != JsonValueKind.Object || !HasUniqueProperties(payloadRoot))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.LicenseMalformed);
        }

        if (!HasOnlyAllowedProperties(payloadRoot, AllowedPayloadProperties))
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.FieldUnknown);
        }

        var document = ReadDocument(payloadRoot);
        if (document is null)
        {
            return LicenseParseOutcome.Failed(LicenseValidationReason.FieldInvalid);
        }

        var envelope = new LicenseSignatureEnvelope
        {
            Algorithm = algorithm,
            KeyId = keyId,
            Signature = signatureBase64
        };

        var signedLicense = new SignedLicense
        {
            Document = document,
            SignedPayload = payloadBytes,
            Signature = envelope
        };

        return LicenseParseOutcome.Success(signedLicense);
    }

    private static JsonDocument? ParseDocument(string text)
    {
        try
        {
            return JsonDocument.Parse(text, DocumentOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasUniqueProperties(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasOnlyAllowedProperties(JsonElement element, string[] allowedNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (Array.IndexOf(allowedNames, property.Name) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetRequiredString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var candidate = property.GetString();
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    private static LicenseDocument? ReadDocument(JsonElement root)
    {
        if (!root.TryGetProperty("licenseVersion", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out var licenseVersion))
        {
            return null;
        }

        if (licenseVersion < LicenseConstants.MinimumSupportedLicenseVersion ||
            licenseVersion > LicenseConstants.MaximumSupportedLicenseVersion)
        {
            return null;
        }

        if (!TryGetRequiredString(root, "licenseId", out var licenseId) ||
            !TryGetRequiredString(root, "product", out var product) ||
            !TryGetRequiredString(root, "edition", out var editionText))
        {
            return null;
        }

        var edition = LicenseEditionExtensions.ParseEdition(editionText);
        if (!edition.IsKnown())
        {
            return null;
        }

        if (!TryGetRequiredString(root, "issuedAt", out var issuedAtText) ||
            !TryParseUtcInstant(issuedAtText, out var issuedAt))
        {
            return null;
        }

        DateTimeOffset? expiresAt = null;
        if (root.TryGetProperty("expiresAt", out var expiresElement) &&
            expiresElement.ValueKind != JsonValueKind.Null)
        {
            if (expiresElement.ValueKind != JsonValueKind.String ||
                !TryParseUtcInstant(expiresElement.GetString(), out var parsedExpiry))
            {
                return null;
            }

            expiresAt = parsedExpiry;
        }

        string? customer = null;
        if (root.TryGetProperty("customer", out var customerElement) &&
            customerElement.ValueKind != JsonValueKind.Null)
        {
            if (customerElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            customer = customerElement.GetString();
        }

        if (!TryReadFeatures(root, out var features))
        {
            return null;
        }

        if (!TryReadLimits(root, out var limits))
        {
            return null;
        }

        return new LicenseDocument
        {
            LicenseVersion = licenseVersion,
            LicenseId = licenseId,
            Product = product,
            Edition = edition,
            Customer = customer,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Features = features,
            Limits = limits
        };
    }

    private static bool TryReadFeatures(JsonElement root, out IReadOnlyList<string> features)
    {
        features = Array.Empty<string>();
        if (!root.TryGetProperty("features", out var featuresElement))
        {
            return true;
        }

        if (featuresElement.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (featuresElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var list = new List<string>();
        foreach (var item in featuresElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = item.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            list.Add(value);
        }

        features = list;
        return true;
    }

    private static bool TryReadLimits(JsonElement root, out IReadOnlyDictionary<string, int> limits)
    {
        limits = new Dictionary<string, int>();
        if (!root.TryGetProperty("limits", out var limitsElement))
        {
            return true;
        }

        if (limitsElement.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (limitsElement.ValueKind != JsonValueKind.Object || !HasUniqueProperties(limitsElement))
        {
            return false;
        }

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var property in limitsElement.EnumerateObject())
        {
            if (string.IsNullOrEmpty(property.Name))
            {
                return false;
            }

            // Integers only: floating-point limit values are rejected.
            if (property.Value.ValueKind != JsonValueKind.Number ||
                !property.Value.TryGetInt32(out var limitValue))
            {
                return false;
            }

            map[property.Name] = limitValue;
        }

        limits = map;
        return true;
    }

    private static bool TryParseUtcInstant(string? value, out DateTimeOffset instant)
    {
        instant = default;
        if (string.IsNullOrEmpty(value) || !value.EndsWith('Z'))
        {
            return false;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out instant);
    }
}