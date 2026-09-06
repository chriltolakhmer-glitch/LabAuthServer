using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using LabAuthServer.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.ActiveDirectory;

[SupportedOSPlatform("windows")]
public sealed class DpapiLdapServiceAccountCredentialProvider : ILdapServiceAccountCredentialProvider
{
    private readonly LdapOptions _ldapOptions;
    private readonly ILogger<DpapiLdapServiceAccountCredentialProvider> _logger;

    public DpapiLdapServiceAccountCredentialProvider(
        IOptions<LdapOptions> ldapOptions,
        ILogger<DpapiLdapServiceAccountCredentialProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapOptions);
        ArgumentNullException.ThrowIfNull(logger);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The LDAP service-account DPAPI secret provider requires Windows.");
        }

        _ldapOptions = ldapOptions.Value ?? throw new ArgumentException("LDAP configuration is required.", nameof(ldapOptions));
        _logger = logger;
    }

    public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = _ldapOptions.ServiceAccountPasswordFile;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("LDAP service-account password is not configured.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException("LDAP service-account password is not configured.");
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(path);
            if (encryptedBytes.Length == 0)
            {
                throw new InvalidOperationException("LDAP service-account password is not configured.");
            }

            var raw = ProtectedData.Unprotect(encryptedBytes, optionalEntropy: null, scope: DataProtectionScope.LocalMachine);
            var password = Encoding.UTF8.GetString(raw);
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("LDAP service-account password is not configured.");
            }

            _logger.LogInformation("LDAP service-account password loaded from protected DPAPI store.");
            return Task.FromResult(password);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError("LDAP service-account password decryption failed for protected DPAPI store.");
            throw new InvalidOperationException("LDAP service-account password is not configured.", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to load LDAP service-account password from protected DPAPI store.");
            throw new InvalidOperationException("LDAP service-account password is not configured.", ex);
        }
    }
}
