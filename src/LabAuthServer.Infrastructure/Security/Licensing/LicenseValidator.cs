using LabAuthServer.Application.Licensing;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Server-side license validator. Applies the Phase 4.5 rule order over the exact license bytes:
/// parse the container, verify the signature over the verbatim payload, then validate semantics
/// (product, edition, time window, features, limits). Every failure path returns a typed deny
/// result and never throws for malformed input. Verification uses only public keys; no private
/// key is required or present.
/// </summary>
public sealed class LicenseValidator : ILicenseValidator
{
    private readonly ILicenseDocumentParser _parser;
    private readonly ILicenseSignatureVerifier _signatureVerifier;
    private readonly ILicenseClock _clock;

    public LicenseValidator(
        ILicenseDocumentParser parser,
        ILicenseSignatureVerifier signatureVerifier,
        ILicenseClock clock)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public LicenseValidationResult Validate(ReadOnlySpan<byte> content)
    {
        try
        {
            if (content.IsEmpty)
            {
                return LicenseValidationResult.Failed(
                    LicenseValidationStatus.Missing, LicenseValidationReason.LicenseMissing);
            }

            // Rules 2-3: parse the container and payload (strict; unknown properties rejected).
            var parseOutcome = _parser.Parse(content);
            if (!parseOutcome.Succeeded)
            {
                var reason = parseOutcome.Failure ?? LicenseValidationReason.LicenseMalformed;
                return LicenseValidationResult.Failed(MapParseStatus(reason), reason);
            }

            var license = parseOutcome.License!;

            // Rules 5-9: algorithm, trusted key and signature verification over the exact bytes.
            var algorithmCheck = CheckAlgorithm(license.Signature.Algorithm);
            if (algorithmCheck is not null)
            {
                return algorithmCheck;
            }

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(license.Signature.Signature);
            }
            catch (FormatException)
            {
                return LicenseValidationResult.Failed(
                    LicenseValidationStatus.InvalidSignature, LicenseValidationReason.SignatureMalformed);
            }

            var verification = _signatureVerifier.Verify(
                license.SignedPayload,
                license.Signature.Algorithm,
                license.Signature.KeyId,
                signatureBytes);

            if (!verification.Succeeded)
            {
                var reason = verification.Failure ?? LicenseValidationReason.SignatureInvalid;
                return LicenseValidationResult.Failed(MapCryptoStatus(reason), reason);
            }

            // Rules 10-16: semantic validation of the now-authenticated document.
            return ValidateDocument(license.Document);
        }
        catch (Exception)
        {
            // Rule 17 / fail-closed: any unexpected failure denies rather than allowing.
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.InvalidConfiguration, LicenseValidationReason.InvalidConfiguration);
        }
    }

    private static LicenseValidationResult? CheckAlgorithm(string algorithm)
    {
        if (!string.Equals(algorithm, LicenseConstants.RsaPssSha256Algorithm, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.InvalidSignature, LicenseValidationReason.AlgorithmUnsupported);
        }

        return null;
    }

    private LicenseValidationResult ValidateDocument(LicenseDocument document)
    {
        // Rule 5: product identity.
        if (!string.Equals(document.Product, LicenseConstants.Product, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.WrongProduct, LicenseValidationReason.ProductMismatch);
        }

        // Rule 6: edition must be a known edition.
        if (!document.Edition.IsKnown())
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.InvalidContent, LicenseValidationReason.FieldInvalid);
        }

        // Rule 8: issuedAt must be usable.
        if (document.IssuedAt == default)
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.NotYetValid, LicenseValidationReason.NotYetValid);
        }

        // Rule 9: expiry semantics. Perpetual is valid; otherwise expiry must follow issuance.
        if (document.ExpiresAt is { } expiresAt)
        {
            if (expiresAt <= document.IssuedAt)
            {
                return LicenseValidationResult.Failed(
                    LicenseValidationStatus.InvalidContent, LicenseValidationReason.ExpiryInvalid);
            }
        }

        // Rule 10: feature identifiers must be structurally valid and within bounds.
        if (!AreFeaturesValid(document.Features))
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.InvalidContent, LicenseValidationReason.FeatureUnknown);
        }

        // Rule 11: limits must be structurally valid and within bounds.
        if (!AreLimitsValid(document.Limits))
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.InvalidContent, LicenseValidationReason.LimitOutOfRange);
        }

        // Rule 15 (time): expiry enforcement with a bounded skew allowance. No grace period.
        var now = _clock.UtcNow;

        if (document.IssuedAt - LicenseValidationPolicy.ClockSkewAllowance > now)
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.NotYetValid, LicenseValidationReason.NotYetValid);
        }

        if (document.ExpiresAt is { } expiry && now >= expiry)
        {
            return LicenseValidationResult.Failed(
                LicenseValidationStatus.Expired, LicenseValidationReason.Expired);
        }

        return LicenseValidationResult.Valid(document);
    }

    private static bool AreFeaturesValid(IReadOnlyList<string> features)
    {
        if (features.Count > LicenseValidationPolicy.MaximumFeatureCount)
        {
            return false;
        }

        foreach (var feature in features)
        {
            if (string.IsNullOrWhiteSpace(feature) ||
                feature.Length > LicenseValidationPolicy.MaximumFeatureLength ||
                !char.IsAsciiLetterLower(feature[0]))
            {
                return false;
            }

            foreach (var character in feature)
            {
                var allowed = char.IsAsciiLetterLower(character)
                    || char.IsAsciiDigit(character)
                    || character is '.' or '-' or '_';

                if (!allowed)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool AreLimitsValid(IReadOnlyDictionary<string, int> limits)
    {
        if (limits.Count > LicenseValidationPolicy.MaximumLimitCount)
        {
            return false;
        }

        foreach (var limit in limits)
        {
            if (string.IsNullOrWhiteSpace(limit.Key) ||
                limit.Key.Length > LicenseValidationPolicy.MaximumLimitKeyLength)
            {
                return false;
            }

            if (limit.Value <= 0 || limit.Value > LicenseValidationPolicy.MaximumLimitValue)
            {
                return false;
            }
        }

        return true;
    }

    private static LicenseValidationStatus MapParseStatus(LicenseValidationReason reason) => reason switch
    {
        LicenseValidationReason.LicenseMissing => LicenseValidationStatus.Missing,
        LicenseValidationReason.LicenseUnreadable => LicenseValidationStatus.Malformed,
        LicenseValidationReason.VersionUnsupported => LicenseValidationStatus.UnsupportedVersion,
        LicenseValidationReason.ProductMismatch => LicenseValidationStatus.WrongProduct,
        LicenseValidationReason.SignatureMalformed => LicenseValidationStatus.InvalidSignature,
        LicenseValidationReason.KeyUntrusted => LicenseValidationStatus.UnknownKey,
        LicenseValidationReason.AlgorithmUnsupported => LicenseValidationStatus.InvalidSignature,
        LicenseValidationReason.SignatureInvalid => LicenseValidationStatus.InvalidSignature,
        LicenseValidationReason.FieldMissing => LicenseValidationStatus.Malformed,
        LicenseValidationReason.FieldUnknown => LicenseValidationStatus.InvalidContent,
        LicenseValidationReason.FieldInvalid => LicenseValidationStatus.InvalidContent,
        _ => LicenseValidationStatus.Malformed
    };

    private static LicenseValidationStatus MapCryptoStatus(LicenseValidationReason reason) => reason switch
    {
        LicenseValidationReason.KeyUntrusted => LicenseValidationStatus.UnknownKey,
        LicenseValidationReason.AlgorithmUnsupported => LicenseValidationStatus.InvalidSignature,
        LicenseValidationReason.InvalidConfiguration => LicenseValidationStatus.InvalidConfiguration,
        _ => LicenseValidationStatus.InvalidSignature
    };
}