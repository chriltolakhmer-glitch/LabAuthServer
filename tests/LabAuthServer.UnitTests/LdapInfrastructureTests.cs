using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;

namespace LabAuthServer.UnitTests;

public sealed class LdapInfrastructureTests
{
    [Fact]
    public void LdapFilterEscaper_EscapesFilterMetacharacters()
    {
        var escaped = LdapFilterEscaper.Escape("user*)(cn=admin)\0");

        Assert.Equal("user\\2A\\29\\28cn=admin\\29\\00", escaped);
    }

    [Fact]
    public void LdapFilterEscaper_PreservesUnicodeCharacters()
    {
        Assert.Equal("jos\u00e9@lab.local", LdapFilterEscaper.Escape("jos\u00e9@lab.local"));
    }

    [Fact]
    public void LdapAuthenticationClient_UsesSuppliedUpnForUserBind()
    {
        const string suppliedUpn = "athens@lab.local";
        const string resolvedDistinguishedName = "CN=athens,CN=Builtin,DC=lab,DC=local";

        var userBindUsername = LdapAuthenticationClient.GetUserBindUsername(
            suppliedUpn,
            resolvedDistinguishedName);

        Assert.Equal(suppliedUpn, userBindUsername);
        Assert.NotEqual(resolvedDistinguishedName, userBindUsername);
    }

    [Fact]
    public void LdapOptionsValidator_RequiresServiceAccountUsernameOnly()
    {
        var options = CreateOptions();
        options.ServiceAccountUsername = "svc-lab-auth@lab.local";
        options.ServiceAccountPassword = string.Empty;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.DoesNotContain(failures, failure => failure.Contains("service-account username and password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LdapService_WithoutServiceAccountSecret_ReturnsSafeRootDseFailure()
    {
        var service = new LdapService(
            Options.Create(CreateOptions()),
            new NullCredentialProvider(),
            NullLogger<LdapService>.Instance);

        var result = await service.QueryRootDseAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("The directory service could not complete the Root DSE query.", result.ErrorMessage);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task DpapiProvider_WithMissingFile_ThrowsConfigurationFailure()
    {
        var provider = new DpapiLdapServiceAccountCredentialProvider(
            Options.Create(new LdapOptions
            {
                ServiceAccountPasswordFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dpapi")
            }),
            NullLogger<DpapiLdapServiceAccountCredentialProvider>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetPasswordAsync());

        Assert.Contains("LDAP service-account password is not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static LdapOptions CreateOptions()
    {
        return new LdapOptions
        {
            Domain = "lab.local",
            Host = "DC01.lab.local",
            Port = 636,
            BaseDn = "DC=lab,DC=local",
            UseLdaps = true,
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            ServiceAccountUsername = "svc-lab-auth@lab.local",
            ServiceAccountPasswordFile = @"C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi"
        };
    }

    private sealed class NullCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("LDAP service-account password is not configured.");
    }
}