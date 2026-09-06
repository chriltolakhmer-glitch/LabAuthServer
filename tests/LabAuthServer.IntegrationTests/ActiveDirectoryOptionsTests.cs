using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Configuration;

namespace LabAuthServer.IntegrationTests;

public sealed class ActiveDirectoryOptionsTests
{
    [Fact]
    public void Validate_WithValidLdapsOptions_ReturnsNoFailures()
    {
        var failures = LdapOptionsValidator.Validate(CreateValidOptions(), isProduction: true);

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_WithoutServiceAccountPassword_ReturnsNoFailure()
    {
        var options = CreateValidOptions();
        options.ServiceAccountPassword = string.Empty;
        options.ServiceAccountUsername = "svc-lab-auth@lab.local";

        var failures = LdapOptionsValidator.Validate(options, isProduction: true);

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_WithMissingDomain_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.Domain = string.Empty;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("domain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithMissingHost_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.Host = string.Empty;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("host is required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("ldap://DC01.lab.local")]
    [InlineData("ldaps://DC01.lab.local")]
    [InlineData("DC01.lab.local:636")]
    public void Validate_WithHostSchemeOrEmbeddedPort_ReturnsFailure(string host)
    {
        var options = CreateValidOptions();
        options.Host = host;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.NotEmpty(failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validate_WithInvalidPort_ReturnsFailure(int port)
    {
        var options = CreateValidOptions();
        options.Port = port;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("port", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("lab.local")]
    [InlineData("DC=lab,,DC=local")]
    public void Validate_WithMissingOrInvalidBaseDn_ReturnsFailure(string baseDn)
    {
        var options = CreateValidOptions();
        options.BaseDn = baseDn;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("base DN", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithMissingUserSearchBaseDn_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.UserSearchBaseDn = string.Empty;

        var failures = LdapOptionsValidator.Validate(options, isProduction: true);

        Assert.Contains(failures, failure => failure.Contains("user search base DN", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UserSearchBaseDn_UsesDomainRootForBuiltinUsers()
    {
        var apiOutputDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiOutputDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var options = configuration
            .GetSection(LdapOptions.SectionName)
            .Get<LdapOptions>();

        Assert.NotNull(options);
        Assert.Equal("DC=lab,DC=local", options.UserSearchBaseDn);

        var athensDistinguishedName = "CN=athens,CN=Builtin,DC=lab,DC=local";
        Assert.EndsWith("," + options.UserSearchBaseDn, athensDistinguishedName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNonPositiveConnectionTimeout_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.ConnectionTimeout = TimeSpan.Zero;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithLdapsAndNonStandardPort_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.Port = 389;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Contains(failures, failure => failure.Contains("636", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithPlainLdapInProduction_ReturnsFailure()
    {
        var options = CreateValidOptions();
        options.UseLdaps = false;
        options.Port = 389;

        var failures = LdapOptionsValidator.Validate(options, isProduction: true);

        Assert.Contains(failures, failure => failure.Contains("Production", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithPlainLdapOutsideProduction_ReturnsNoFailures()
    {
        var options = CreateValidOptions();
        options.UseLdaps = false;
        options.Port = 389;

        var failures = LdapOptionsValidator.Validate(options, isProduction: false);

        Assert.Empty(failures);
    }

    private static LdapOptions CreateValidOptions()
    {
        return new LdapOptions
        {
            Domain = "lab.local",
            Host = "DC01.lab.local",
            Port = 636,
            BaseDn = "DC=lab,DC=local",
            UserSearchBaseDn = "DC=lab,DC=local",
            UseLdaps = true,
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            ServiceAccountUsername = "svc-lab-auth@lab.local",
            ServiceAccountPasswordFile = @"C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi"
        };
    }
}
