using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Security;

public sealed class CertificateSigningKeyProvider : IProtectedSigningKeyProvider
{
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
                "Signing certificate lookup failed for key identifier {KeyIdentifier} in store {StoreName} at {StoreLocation} using thumbprint {Thumbprint}.",
                keyIdentifier,
                options.SigningCertificateStoreName,
                options.SigningCertificateStoreLocation,
                options.SigningCertificateThumbprint);
            throw;
        }

        if (certificate is null)
        {
            _logger?.LogError(
                "Signing certificate was not found for key identifier {KeyIdentifier} in store {StoreName} at {StoreLocation} using thumbprint {Thumbprint}.",
                keyIdentifier,
                options.SigningCertificateStoreName,
                options.SigningCertificateStoreLocation,
                options.SigningCertificateThumbprint);
            throw new InvalidOperationException("The configured signing certificate could not be resolved from the certificate store.");
        }

        if (!certificate.HasPrivateKey)
        {
            _logger?.LogError(
                "Signing certificate found but does not contain a private key for key identifier {KeyIdentifier} in store {StoreName} at {StoreLocation} using thumbprint {Thumbprint}.",
                keyIdentifier,
                options.SigningCertificateStoreName,
                options.SigningCertificateStoreLocation,
                options.SigningCertificateThumbprint);
            throw new InvalidOperationException("The configured signing certificate does not contain a private key.");
        }

        var privateKey = certificate.GetRSAPrivateKey();
        if (privateKey is null)
        {
            _logger?.LogError(
                "Signing certificate private key is not an RSA key for key identifier {KeyIdentifier} in store {StoreName} at {StoreLocation} using thumbprint {Thumbprint}.",
                keyIdentifier,
                options.SigningCertificateStoreName,
                options.SigningCertificateStoreLocation,
                options.SigningCertificateThumbprint);
            throw new InvalidOperationException("The configured signing certificate does not expose an RSA private key.");
        }

        return ValueTask.FromResult(new SigningKeyMaterial(keyIdentifier, privateKey));
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
        var certificate = certificates.Count > 0 ? certificates[0] : null;
        if (certificate is null)
        {
            throw new InvalidOperationException("The configured signing certificate thumbprint was not found in the certificate store.");
        }

        return certificate;
    }
}
