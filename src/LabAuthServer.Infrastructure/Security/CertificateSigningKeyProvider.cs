using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security;

public sealed class CertificateSigningKeyProvider : IProtectedSigningKeyProvider
{
    internal const int MinimumRsaKeySize = 2048;

    private readonly IOptions<TokenOptions> _tokenOptions;
    private readonly Func<TokenOptions, string, X509Certificate2> _certificateResolver;
    private readonly ILogger<CertificateSigningKeyProvider>? _logger;

    public CertificateSigningKeyProvider(IOptions<TokenOptions> tokenOptions)
        : this(tokenOptions, ResolveCertificate, null)
    {
    }

    public CertificateSigningKeyProvider(
        IOptions<TokenOptions> tokenOptions,
        ILogger<CertificateSigningKeyProvider>? logger = null)
        : this(tokenOptions, ResolveCertificate, logger)
    {
    }

    public CertificateSigningKeyProvider(
        IOptions<TokenOptions> tokenOptions,
        Func<TokenOptions, X509Certificate2> certificateResolver,
        ILogger<CertificateSigningKeyProvider>? logger = null)
    {
        _tokenOptions = tokenOptions ?? throw new ArgumentNullException(nameof(tokenOptions));
        ArgumentNullException.ThrowIfNull(certificateResolver);
        _certificateResolver = (options, _) => certificateResolver(options);
        _logger = logger;
    }

    public CertificateSigningKeyProvider(
        IOptions<TokenOptions> tokenOptions,
        Func<TokenOptions, string, X509Certificate2> certificateResolver,
        ILogger<CertificateSigningKeyProvider>? logger = null)
        : this(tokenOptions, certificateResolver, logger, true)
    {
    }

    private CertificateSigningKeyProvider(
        IOptions<TokenOptions> tokenOptions,
        Func<TokenOptions, string, X509Certificate2> certificateResolver,
        ILogger<CertificateSigningKeyProvider>? logger,
        bool keyAwareResolver)
    {
        _tokenOptions = tokenOptions ?? throw new ArgumentNullException(nameof(tokenOptions));
        _certificateResolver = certificateResolver ?? throw new ArgumentNullException(nameof(certificateResolver));
        _logger = logger;
    }

    public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = _tokenOptions.Value;

        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            if (failures.Any(failure => failure.Contains("certificate", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Signing certificate configuration is invalid or the configured certificate was not found in the certificate store.");
            }

            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        return GetKeyAsync(options.ActiveKeyId, cancellationToken);
    }

    public ValueTask<SigningKeyMaterial> GetKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new InvalidOperationException("The signing key identifier is required.");
        }

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            if (failures.Any(failure => failure.Contains("certificate", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Signing certificate configuration is invalid or the configured certificate was not found in the certificate store.");
            }

            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        var isActiveKey = string.Equals(options.ActiveKeyId, keyIdentifier, StringComparison.Ordinal);
        var isPreviousKey = !string.IsNullOrWhiteSpace(options.PreviousKeyId) &&
            string.Equals(options.PreviousKeyId, keyIdentifier, StringComparison.Ordinal);

        if (!isActiveKey && (!isPreviousKey || !options.PreviousKeyExpiresAt.HasValue || options.PreviousKeyExpiresAt.Value <= DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("The requested signing key identifier is not approved or is unavailable.");
        }

        X509Certificate2 certificate;
        try
        {
            certificate = _certificateResolver(options, keyIdentifier);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Signing certificate lookup failed for key identifier {KeyIdentifier}.",
                keyIdentifier);
            throw;
        }

        ValidateCertificate(certificate, requirePrivateKey: true);

        var privateKey = certificate.GetRSAPrivateKey();
        ArgumentNullException.ThrowIfNull(privateKey);

        return ValueTask.FromResult(new SigningKeyMaterial(keyIdentifier, privateKey));
    }

    public ValueTask<ValidationKeyMaterial> GetValidationKeyAsync(
        string keyIdentifier,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyIdentifier))
        {
            throw new InvalidOperationException("The signing key identifier is required.");
        }

        var options = _tokenOptions.Value;
        var failures = TokenOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Signing-key configuration is invalid.");
        }

        var isActiveKey = string.Equals(options.ActiveKeyId, keyIdentifier, StringComparison.Ordinal);
        var isPreviousKey = string.Equals(options.PreviousKeyId, keyIdentifier, StringComparison.Ordinal);
        if (!isActiveKey && (!isPreviousKey || !options.PreviousKeyExpiresAt.HasValue || options.PreviousKeyExpiresAt.Value <= DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("The requested signing key identifier is not approved or is unavailable.");
        }

        var certificate = ResolveAndValidateCertificate(options, keyIdentifier, requirePrivateKey: false);
        var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null)
        {
            throw new InvalidOperationException("The configured signing certificate does not expose an RSA public key.");
        }

        return ValueTask.FromResult(new ValidationKeyMaterial(keyIdentifier, publicKey));
    }

    public static string NormalizeThumbprint(string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return string.Empty;
        }

        var normalized = new string(thumbprint
            .Where(ch => !char.IsWhiteSpace(ch))
            .ToArray());

        return normalized.Trim();
    }

    private static X509Certificate2 ResolveCertificate(TokenOptions options, string keyIdentifier)
    {
        var storeLocation = options.SigningCertificateStoreLocation;
        var storeName = options.SigningCertificateStoreName;
        var thumbprint = string.Equals(options.ActiveKeyId, keyIdentifier, StringComparison.Ordinal)
            ? NormalizeThumbprint(options.SigningCertificateThumbprint)
            : NormalizeThumbprint(options.PreviousSigningCertificateThumbprint);

        if (string.IsNullOrWhiteSpace(storeLocation) || string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(thumbprint))
        {
            throw new InvalidOperationException("The signing certificate configuration is missing store or thumbprint information.");
        }

        using var store = new X509Store(storeName, Enum.Parse<StoreLocation>(storeLocation, ignoreCase: true));
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        return SelectCertificate(certificates, DateTimeOffset.UtcNow);
    }

    internal static X509Certificate2 SelectCertificate(
        IEnumerable<X509Certificate2> certificates,
        DateTimeOffset now)
    {
        var validCertificates = certificates
            .Where(certificate => certificate.NotBefore.ToUniversalTime() <= now.UtcDateTime &&
                                  certificate.NotAfter.ToUniversalTime() >= now.UtcDateTime)
            .ToArray();

        if (validCertificates.Length == 0)
        {
            throw new InvalidOperationException("No currently valid signing certificate matched the configured key.");
        }

        if (validCertificates.Length != 1)
        {
            throw new InvalidOperationException("Multiple currently valid signing certificates matched the configured key.");
        }

        return validCertificates[0];
    }

    private X509Certificate2 ResolveAndValidateCertificate(
        TokenOptions options,
        string keyIdentifier,
        bool requirePrivateKey)
    {
        var certificate = _certificateResolver(options, keyIdentifier);
        ValidateCertificate(certificate, requirePrivateKey);
        return certificate;
    }

    private static void ValidateCertificate(X509Certificate2 certificate, bool requirePrivateKey)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var now = DateTimeOffset.UtcNow;
        if (certificate.NotBefore.ToUniversalTime() > now.UtcDateTime ||
            certificate.NotAfter.ToUniversalTime() < now.UtcDateTime)
        {
            throw new InvalidOperationException("The signing certificate is not currently valid.");
        }

        using var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null)
        {
            throw new InvalidOperationException("The signing certificate does not contain an RSA key.");
        }

        if (publicKey.KeySize < MinimumRsaKeySize)
        {
            throw new InvalidOperationException("The signing certificate RSA key is too small.");
        }

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
        if (keyUsage is not null && !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
        {
            throw new InvalidOperationException("The signing certificate does not allow digital signatures.");
        }

        // JWT signing uses a general-purpose signing certificate; an EKU extension is rejected
        // rather than treating an unrelated server/client authentication purpose as sufficient.
        if (certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Any())
        {
            throw new InvalidOperationException("The signing certificate must not contain an incompatible EKU extension.");
        }

        if (requirePrivateKey && !certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("The signing certificate does not contain a private key.");
        }
    }
}
