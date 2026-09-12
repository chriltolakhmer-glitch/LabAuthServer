using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Licensing;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Security;
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;

namespace LabAuthServer.IntegrationTests;

public sealed class TestInfrastructureIsolationTests
{
    [Fact]
    public async Task OrdinaryFactory_RecordsProtectedAccessThroughProductionAuditValidator()
    {
        using var factory = new TestApiFactory();
        using var scope = factory.Services.CreateScope();
        Assert.Same(factory.Audit, scope.ServiceProvider.GetRequiredService<IAuditEventService>());
        var token = await scope.ServiceProvider.GetRequiredService<ITokenService>().IssueAsync(
            new TokenIssuanceRequest { Subject = "reader@test.invalid", Roles = ["Reader"], Scopes = [] });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await client.GetAsync("/api/v1/protected");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Audit.Events);
        Assert.Equal(AuditEventTypes.AccessGranted, recorded.EventTypeCode);
        Assert.Empty(AuditEventValidator.Validate(recorded));
        Assert.Empty(factory.Audit.ValidationFailures);
    }

    [Fact]
    public async Task RecordingAudit_RejectsSensitiveEventWithoutRecordingIt()
    {
        var audit = new RecordingAuditEventService();
        await Assert.ThrowsAsync<ArgumentException>(() => audit.WriteAsync(new AuditEvent
        {
            EventTypeCode = AuditEventTypes.LoginFailure, CorrelationId = Guid.NewGuid(),
            DetailsJson = "{\"password\":\"synthetic\"}"
        }));
        Assert.Empty(audit.Events);
        Assert.NotEmpty(audit.ValidationFailures);
    }

    [Fact]
    public async Task DefaultHost_BlocksCredentialConnectionsAndCertificateAccess()
    {
        using var factory = new InfrastructureSafeApiFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        Assert.Same(factory.Audit, services.GetRequiredService<IAuditEventService>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => services.GetRequiredService<ILdapServiceAccountCredentialProvider>().GetPasswordAsync());
        Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<ILdapConnectionFactory>().Create(new LdapOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await services.GetRequiredService<IProtectedSigningKeyProvider>().GetActiveKeyAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await services.GetRequiredService<IProtectedSigningKeyProvider>().GetValidationKeyAsync("host-key"));
    }

    [Fact]
    public void OrdinaryHost_DoesNotLoadEnvironmentLicenseFilesOrTrustedKeys()
    {
        using var factory = new InfrastructureSafeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<LicenseValidationOptions>(options =>
            {
                options.LicenseFilePath = "environment-license-must-not-be-read";
                options.TrustedKeys.Add(new TrustedLicenseKeyOptions { KeyId = "environment-key", PublicKey = "not-a-key" });
            })));
        var options = configured.Services.GetRequiredService<IOptions<LicenseValidationOptions>>().Value;
        Assert.Empty(options.LicenseFilePath);
        Assert.Empty(options.TrustedKeys);
        Assert.True(configured.Services.GetRequiredService<ILicensePolicyProvider>().GetPolicy().IsRestricted);
    }

    [Theory]
    [InlineData("HOST")]
    [InlineData("DOMAIN")]
    [InlineData("BASE_DN")]
    [InlineData("USERNAME")]
    [InlineData("PASSWORD_FILE")]
    public void ExplicitLdapAcceptance_RequiresEveryTargetValue(string missing)
    {
        string? Read(string name) => name == LdapAcceptanceFactAttribute.EnableVariable ? "1"
            : name == "LABAUTHSERVER_LDAP_TEST_" + missing ? null : "explicit";
        var failure = Assert.Throws<InvalidOperationException>(() => LdapAcceptanceTests.RequireOptions(Read));
        Assert.Contains("LABAUTHSERVER_LDAP_TEST_" + missing, failure.Message);
    }

    [Fact]
    public void LdapTargetAloneDoesNotEnableAcceptance()
        => Assert.Throws<InvalidOperationException>(() => LdapAcceptanceTests.RequireOptions(_ => "explicit-target"));
}
