using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class CertificateSigningKeyProviderTests
{
    [Fact]
    public async Task GetKeyAsync_WithThumbprintMatchAndPrivateKey_ReturnsValidKey()
    {
        using var certificate = CreateCertificate();
        var provider = new CertificateSigningKeyProvider(
            Options.Create(new TokenOptions
            {
                Issuer = "https://DC01.lab.local",
                Audience = "LabAuthServer.API",
                AccessTokenLifetime = TimeSpan.FromHours(1),
                ClockSkew = TimeSpan.FromMinutes(5),
                SigningAlgorithm = "RS256",
                ActiveKeyId = "thumbprint-key",
                SigningCertificateStoreLocation = "LocalMachine",
                SigningCertificateStoreName = "My",
                SigningCertificateThumbprint = certificate.Thumbprint,
                SigningKeyStoreReference = $"Cert:\\LocalMachine\\My\\{certificate.Thumbprint}",
                MaximumClaimSize = 4096,
                MaximumTokenSize = 16384
            }),
            _ => certificate);

        using var material = await provider.GetKeyAsync("thumbprint-key");

        Assert.Equal("thumbprint-key", material.KeyIdentifier);
        Assert.NotNull(material.PrivateKey);
    }

    [Fact]
    public async Task GetKeyAsync_UsesCertificatePrivateKeyDirectlyWithoutPkcs12RoundTrip()
    {
        using var certificate = CreateCertificate();
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        using var material = await provider.GetKeyAsync("thumbprint-key");

        Assert.Equal(certificate.GetRSAPrivateKey()!.ExportParameters(false).Modulus,
            material.PrivateKey.ExportParameters(false).Modulus);
    }

    [Fact]
    public async Task GetKeyAsync_WithPreviousKeyId_ResolvesPreviousCertificate()
    {
        using var activeCertificate = CreateCertificate();
        using var previousCertificate = CreateCertificate();
        var options = CreateOptions(activeCertificate.Thumbprint);
        options.PreviousKeyId = "previous-key";
        options.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        options.PreviousSigningCertificateThumbprint = previousCertificate.Thumbprint;
        var provider = new CertificateSigningKeyProvider(
            Options.Create(options),
            (configuredOptions, keyIdentifier) => keyIdentifier == options.ActiveKeyId
                ? activeCertificate
                : previousCertificate);

        using var material = await provider.GetKeyAsync("previous-key");

        Assert.Equal("previous-key", material.KeyIdentifier);
        Assert.Equal(
            previousCertificate.GetRSAPrivateKey()!.ExportParameters(false).Modulus,
            material.PrivateKey.ExportParameters(false).Modulus);
    }

    [Fact]
    public async Task GetKeyAsync_WithExpiredPreviousKey_RejectsKey()
    {
        using var certificate = CreateCertificate();
        var options = CreateOptions(certificate.Thumbprint);
        options.PreviousKeyId = "previous-key";
        options.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        options.PreviousSigningCertificateThumbprint = certificate.Thumbprint;
        var provider = new CertificateSigningKeyProvider(
            Options.Create(options),
            (_, _) => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("previous-key").AsTask());

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_WithPreviousKeyIdAndMissingCertificate_RejectsKey()
    {
        using var certificate = CreateCertificate();
        var options = CreateOptions(certificate.Thumbprint);
        options.PreviousKeyId = "previous-key";
        options.PreviousKeyExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var provider = new CertificateSigningKeyProvider(
            Options.Create(options),
            (_, _) => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("previous-key").AsTask());

        Assert.Contains("certificate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_WhenThumbprintNotFound_ThrowsClearInvalidOperationException()
    {
        var provider = new CertificateSigningKeyProvider(
            Options.Create(new TokenOptions
            {
                Issuer = "https://DC01.lab.local",
                Audience = "LabAuthServer.API",
                AccessTokenLifetime = TimeSpan.FromHours(1),
                ClockSkew = TimeSpan.FromMinutes(5),
                SigningAlgorithm = "RS256",
                ActiveKeyId = "thumbprint-key",
                SigningCertificateStoreLocation = "LocalMachine",
                SigningCertificateStoreName = "My",
                SigningCertificateThumbprint = "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ",
                SigningKeyStoreReference = "Cert:\\LocalMachine\\My\\ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ",
                MaximumClaimSize = 4096,
                MaximumTokenSize = 16384
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("certificate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_WhenCertificateHasNoPrivateKey_ThrowsClearInvalidOperationException()
    {
        using var certificate = CreateCertificate(includePrivateKey: false);
        var provider = new CertificateSigningKeyProvider(
            Options.Create(new TokenOptions
            {
                Issuer = "https://DC01.lab.local",
                Audience = "LabAuthServer.API",
                AccessTokenLifetime = TimeSpan.FromHours(1),
                ClockSkew = TimeSpan.FromMinutes(5),
                SigningAlgorithm = "RS256",
                ActiveKeyId = "thumbprint-key",
                SigningCertificateStoreLocation = "LocalMachine",
                SigningCertificateStoreName = "My",
                SigningCertificateThumbprint = certificate.Thumbprint,
                SigningKeyStoreReference = $"Cert:\\LocalMachine\\My\\{certificate.Thumbprint}",
                MaximumClaimSize = 4096,
                MaximumTokenSize = 16384
            }),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("private key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetValidationKeyAsync_AllowsCertificateWithoutPrivateKey()
    {
        using var certificate = CreateCertificate(includePrivateKey: false);
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        using var material = await provider.GetValidationKeyAsync("thumbprint-key");

        Assert.NotNull(material.PublicKey);
        Assert.Equal(2048, material.PublicKey.KeySize);
        Assert.ThrowsAny<CryptographicException>(() => material.PublicKey.ExportParameters(true));
    }

    [Fact]
    public async Task GetKeyAsync_RejectsExpiredCertificate()
    {
        using var certificate = CreateCertificate(DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-1));
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("valid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetValidationKeyAsync_RejectsNotYetValidCertificate()
    {
        using var certificate = CreateCertificate(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2));
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetValidationKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("valid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectCertificate_RejectsDuplicateCurrentlyValidCertificates()
    {
        using var first = CreateCertificate();
        using var second = CreateCertificate();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CertificateSigningKeyProvider.SelectCertificate(
                [first, second],
                DateTimeOffset.UtcNow));

        Assert.Contains("Multiple", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetKeyAsync_RejectsCertificateWithoutDigitalSignatureUsage()
    {
        using var certificate = CreateCertificate(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            X509KeyUsageFlags.KeyEncipherment);
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("digital signatures", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_RejectsInsufficientRsaKeySize()
    {
        using var certificate = CreateCertificate(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            keySize: 1024);
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("too small", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetKeyAsync_RejectsCertificateWithEkuExtension()
    {
        using var certificate = CreateCertificate(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            extendedKeyUsageOid: "1.3.6.1.5.5.7.3.1");
        var provider = new CertificateSigningKeyProvider(
            Options.Create(CreateOptions(certificate.Thumbprint)),
            _ => certificate);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetKeyAsync("thumbprint-key").AsTask());

        Assert.Contains("EKU", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeThumbprint_RemovesWhitespaceAndNormalizesCase()
    {
        var normalized = CertificateSigningKeyProvider.NormalizeThumbprint(" BD5 45BA 289E BFC6 45C8 C3DC 4243 1197 5579D7E09 ");

        Assert.Equal("BD545BA289EBFC645C8C3DC424311975579D7E09", normalized);
    }

    private static X509Certificate2 CreateCertificate(
        bool includePrivateKey = true)
        => CreateCertificate(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            keyUsage: null,
            includePrivateKey: includePrivateKey);

    private static X509Certificate2 CreateCertificate(
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        X509KeyUsageFlags? keyUsage = null,
        string? extendedKeyUsageOid = null,
        int keySize = 2048,
        bool includePrivateKey = true)
    {
        using var rsa = RSA.Create(keySize);
        var request = new CertificateRequest("CN=LabAuthServer.Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        if (keyUsage.HasValue)
        {
            request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage.Value, critical: true));
        }

        if (extendedKeyUsageOid is not null)
        {
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                [new Oid(extendedKeyUsageOid)],
                critical: true));
        }

        var cert = request.CreateSelfSigned(notBefore, notAfter);

        if (!includePrivateKey)
        {
            return X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
        }

        return cert;
    }

    private static TokenOptions CreateOptions(string thumbprint)
    {
        return new TokenOptions
        {
            Issuer = "https://DC01.lab.local",
            Audience = "LabAuthServer.API",
            AccessTokenLifetime = TimeSpan.FromHours(1),
            ClockSkew = TimeSpan.FromMinutes(5),
            SigningAlgorithm = "RS256",
            ActiveKeyId = "thumbprint-key",
            SigningCertificateStoreLocation = "LocalMachine",
            SigningCertificateStoreName = "My",
            SigningCertificateThumbprint = thumbprint,
            SigningKeyStoreReference = $"Cert:\\LocalMachine\\My\\{thumbprint}",
            MaximumClaimSize = 4096,
            MaximumTokenSize = 16384
        };
    }
}
