namespace LabAuthServer.Infrastructure.Security;

public static class TokenOptionsValidator
{
    public static IReadOnlyList<string> Validate(TokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) ||
            !string.Equals(issuer.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            issuer.AbsolutePath != "/" && issuer.AbsolutePath.Length > 0)
        {
            failures.Add("Token issuer must be an absolute HTTPS URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience) || options.Audience.Any(char.IsWhiteSpace))
        {
            failures.Add("Token audience is required and must not contain whitespace.");
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero || options.AccessTokenLifetime > TimeSpan.FromDays(1))
        {
            failures.Add("Access-token lifetime must be greater than zero and no more than one day.");
        }

        if (options.ClockSkew < TimeSpan.Zero || options.ClockSkew > TimeSpan.FromMinutes(5))
        {
            failures.Add("Token clock skew must be between zero and five minutes.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningAlgorithm) ||
            (!options.SigningAlgorithm.StartsWith("RS", StringComparison.Ordinal) &&
             !options.SigningAlgorithm.StartsWith("PS", StringComparison.Ordinal)))
        {
            failures.Add("Token signing algorithm must be an RSA-compatible RS or PS algorithm.");
        }

        ValidateIdentifier(options.ActiveKeyId, "active signing key identifier", failures);

        if (!string.IsNullOrWhiteSpace(options.PreviousKeyId))
        {
            ValidateIdentifier(options.PreviousKeyId, "previous signing key identifier", failures);

            if (string.Equals(options.ActiveKeyId, options.PreviousKeyId, StringComparison.Ordinal))
            {
                failures.Add("The active and previous signing key identifiers must not be the same.");
            }

            if (options.PreviousKeyExpiresAt.HasValue &&
                options.PreviousKeyExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                failures.Add("The previous signing key overlap expiry must be in the future.");
            }
        }

        if (options.ApprovedKeyIds.Count > 0)
        {
            var approved = options.ApprovedKeyIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .ToArray();

            if (approved.Length != options.ApprovedKeyIds.Count ||
                approved.Distinct(StringComparer.Ordinal).Count() != approved.Length)
            {
                failures.Add("Approved signing key identifiers must be unique and non-empty.");
            }

            if (!string.IsNullOrWhiteSpace(options.ActiveKeyId) &&
                !approved.Contains(options.ActiveKeyId, StringComparer.Ordinal))
            {
                failures.Add("The active signing key identifier must be present in the approved key list.");
            }

            if (!string.IsNullOrWhiteSpace(options.PreviousKeyId) &&
                !approved.Contains(options.PreviousKeyId, StringComparer.Ordinal))
            {
                failures.Add("The previous signing key identifier must be present in the approved key list.");
            }
        }

        var certificateLocation = (options.SigningCertificateStoreLocation ?? string.Empty).Trim();
        var certificateStoreName = (options.SigningCertificateStoreName ?? string.Empty).Trim();
        var certificateThumbprint = (options.SigningCertificateThumbprint ?? string.Empty).Trim();
        var previousCertificateThumbprint = (options.PreviousSigningCertificateThumbprint ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(certificateThumbprint) ||
            !string.IsNullOrWhiteSpace(certificateLocation) ||
            !string.IsNullOrWhiteSpace(certificateStoreName))
        {
            if (!string.Equals(certificateLocation, "CurrentUser", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(certificateLocation, "LocalMachine", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Signing certificate store location must be CurrentUser or LocalMachine.");
            }

            if (string.IsNullOrWhiteSpace(certificateStoreName))
            {
                failures.Add("Signing certificate store name is required when certificate-store signing is configured.");
            }

            var normalizedThumbprint = CertificateSigningKeyProvider.NormalizeThumbprint(certificateThumbprint);
            if (string.IsNullOrWhiteSpace(normalizedThumbprint) || normalizedThumbprint.Length != 40 || normalizedThumbprint.Any(ch => !Uri.IsHexDigit(ch)))
            {
                failures.Add("Signing certificate thumbprint must be a 40-character hexadecimal value.");
            }

            if (!string.IsNullOrWhiteSpace(previousCertificateThumbprint))
            {
                var normalizedPreviousThumbprint = CertificateSigningKeyProvider.NormalizeThumbprint(previousCertificateThumbprint);
                if (normalizedPreviousThumbprint.Length != 40 || normalizedPreviousThumbprint.Any(ch => !Uri.IsHexDigit(ch)))
                {
                    failures.Add("Previous signing certificate thumbprint must be a 40-character hexadecimal value.");
                }

                if (string.Equals(normalizedThumbprint, normalizedPreviousThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("The active and previous signing certificate thumbprints must not be the same.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(options.PreviousKeyId))
            {
                failures.Add("Previous signing certificate thumbprint is required when a previous signing key is configured.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(previousCertificateThumbprint))
        {
            failures.Add("Previous signing certificate thumbprint requires active certificate-store signing configuration.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKeyStoreReference) ||
            options.SigningKeyStoreReference.Contains("BEGIN", StringComparison.OrdinalIgnoreCase) ||
            options.SigningKeyStoreReference.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Signing key store reference is required and must not contain key material.");
        }

        if (options.MaximumClaimSize <= 0)
        {
            failures.Add("Maximum claim size must be greater than zero.");
        }

        if (options.MaximumTokenSize <= 0)
        {
            failures.Add("Maximum token size must be greater than zero.");
        }
        else if (options.MaximumTokenSize < options.MaximumClaimSize)
        {
            failures.Add("Maximum token size must be at least the maximum claim size.");
        }

        return failures;
    }

    private static void ValidateIdentifier(string value, string name, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(char.IsWhiteSpace))
        {
            failures.Add($"Token {name} is required, must be no more than 128 characters, and must not contain whitespace.");
        }
    }
}
