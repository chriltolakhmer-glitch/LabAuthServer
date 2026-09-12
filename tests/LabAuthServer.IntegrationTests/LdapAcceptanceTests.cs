using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapAcceptanceTests
{
    [LdapAcceptanceFact]
    [Trait("Category", "LdapAcceptance")]
    public async Task ExplicitRootDseTarget_MustConnectAndReturnAttributes()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DPAPI/LDAPS acceptance requires an authorized Windows environment.");
        var options = Options.Create(RequireOptions(Environment.GetEnvironmentVariable));
        var credentials = new DpapiLdapServiceAccountCredentialProvider(options,
            NullLogger<DpapiLdapServiceAccountCredentialProvider>.Instance);
        var service = new LdapService(options, credentials, NullLogger<LdapService>.Instance, new LdapConnectionFactory());
        var result = await service.QueryRootDseAsync();
        Assert.True(result.IsSuccess, "The explicitly selected LDAPS acceptance target did not succeed.");
        Assert.NotNull(result.Attributes);
        Assert.NotEmpty(result.Attributes);
        Assert.Null(result.ErrorMessage);
    }

    internal static LdapOptions RequireOptions(Func<string, string?> readEnvironment)
    {
        if (!string.Equals(readEnvironment(LdapAcceptanceFactAttribute.EnableVariable), "1", StringComparison.Ordinal))
            throw new InvalidOperationException("Real LDAP acceptance must be explicitly enabled.");
        string Require(string suffix)
        {
            var name = "LABAUTHSERVER_LDAP_TEST_" + suffix;
            var value = readEnvironment(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{name} is required; application defaults are not used.");
            return value;
        }
        var options = new LdapOptions
        {
            Host = Require("HOST"), Domain = Require("DOMAIN"),
            BaseDn = Require("BASE_DN"), UserSearchBaseDn = Require("BASE_DN"),
            ServiceAccountUsername = Require("USERNAME"), ServiceAccountPasswordFile = Require("PASSWORD_FILE"),
            UseLdaps = true, Port = 636, ConnectionTimeout = TimeSpan.FromSeconds(10)
        };
        if (LdapOptionsValidator.Validate(options, isProduction: true).Count != 0)
            throw new InvalidOperationException("Explicit LDAP acceptance configuration is invalid.");
        return options;
    }
}
