using System.Security.Cryptography;
using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Offline vendor-side license issuer. Validates an issuance request, builds the license document,
/// serializes exactly one canonical payload, signs it with RSA-PSS/SHA-256 using the key supplied
/// by <see cref="ILicenseSigningKeyProvider"/>, and returns a license container compatible with the
/// server's parser and verifier.
/// The issuer holds no private key of its own, is not referenced by any production server project,
/// and is not reachable from the application request pipeline.
/// </summary>
public sealed class LicenseIssuer
{
    private readonly ILicenseSigningKeyProvider _keyProvider;
    private readonly ILicenseSigner _signer;

    /// <summary>Creates an issuer. The default signer uses RSA-PSS with SHA-256.</summary>
    public LicenseIssuer(ILicenseSigningKeyProvider keyProvider, ILicenseSigner? signer = null)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        _signer = signer ?? new RsaPssLicenseSigner();
    }

    /// <summary>Issues a signed license from a validated request.</summary>
    public LicenseIssuanceResult Issue(LicenseIssuanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.LicenseId))
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.LicenseIdMissing);
        }

        if (!string.Equals(request.Product, LicenseConstants.Product, StringComparison.Ordinal))
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.ProductInvalid);
        }

        if (!request.Edition.IsKnown())
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.EditionUnknown);
        }

        if (request.IssuedAt == default)
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.IssuedAtInvalid);
        }

        if (string.IsNullOrWhiteSpace(request.KeyId))
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.KeyIdMissing);
        }

        if (!string.Equals(request.KeyId, _keyProvider.KeyId, StringComparison.Ordinal))
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.KeyIdMismatch);
        }

        var issuedAt = NormalizeToSecond(request.IssuedAt);

        DateTimeOffset? expiresAt = null;
        if (request.ExpiresAt is { } requestedExpiry)
        {
            if (requestedExpiry == default)
            {
                return LicenseIssuanceResult.Failed(LicenseIssuanceReason.ExpiresAtInvalid);
            }

            var normalizedExpiry = NormalizeToSecond(requestedExpiry);
            if (normalizedExpiry <= issuedAt)
            {
                return LicenseIssuanceResult.Failed(LicenseIssuanceReason.ExpiresAtBeforeIssuedAt);
            }

            expiresAt = normalizedExpiry;
        }

        foreach (var feature in request.Features)
        {
            if (!IsValidFeatureIdentifier(feature))
            {
                return LicenseIssuanceResult.Failed(LicenseIssuanceReason.FeatureInvalid);
            }
        }

        foreach (var limit in request.Limits)
        {
            if (!IsValidLimit(limit.Key, limit.Value))
            {
                return LicenseIssuanceResult.Failed(LicenseIssuanceReason.LimitInvalid);
            }
        }

        var signingKey = _keyProvider.TryGetSigningKey();
        if (signingKey is null)
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.SigningKeyUnavailable);
        }

        if (signingKey.KeySize < LicenseConstants.MinimumRsaKeySize)
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.SigningKeyPolicyViolation);
        }

        var document = new LicenseDocument
        {
            LicenseVersion = LicenseConstants.MinimumSupportedLicenseVersion,
            LicenseId = request.LicenseId,
            Product = LicenseConstants.Product,
            Edition = request.Edition,
            Customer = request.Customer,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Features = request.Features.ToArray(),
            Limits = new Dictionary<string, int>(request.Limits, StringComparer.Ordinal)
        };

        byte[] payload;
        try
        {
            payload = LicensePayloadSerializer.Serialize(document);
        }
        catch (Exception)
        {
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.PayloadSerializationFailed);
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = _signer.Sign(payload, signingKey);
        }
        catch (CryptographicException)
        {
            // A key without private signing capability fails here rather than producing a license.
            return LicenseIssuanceResult.Failed(LicenseIssuanceReason.SigningKeyUnavailable);
        }

        var envelope = new LicenseSignatureEnvelope
        {
            Algorithm = LicenseConstants.RsaPssSha256Algorithm,
            KeyId = request.KeyId,
            Signature = Convert.ToBase64String(signatureBytes)
        };

        var signedLicense = new SignedLicense
        {
            Document = document,
            SignedPayload = payload,
            Signature = envelope
        };

        var containerBytes = LicenseContainerSerializer.Serialize(signedLicense);
        return LicenseIssuanceResult.Success(signedLicense, containerBytes);
    }

    private static DateTimeOffset NormalizeToSecond(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);
    }

    private static bool IsValidFeatureIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !char.IsAsciiLetterLower(value[0]))
        {
            return false;
        }

        foreach (var character in value)
        {
            var allowed = char.IsAsciiLetterLower(character)
                || char.IsAsciiDigit(character)
                || character is '.' or '-' or '_';

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidLimit(string key, int value)
        => !string.IsNullOrWhiteSpace(key) && value > 0;
}