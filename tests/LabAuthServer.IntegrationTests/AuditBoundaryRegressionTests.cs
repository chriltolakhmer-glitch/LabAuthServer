using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LabAuthServer.IntegrationTests;

public sealed class AuditBoundaryRegressionTests
{
    [Theory]
    [InlineData(AuthenticationFailureCategory.InvalidCredentials, 401)]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, 503)]
    [InlineData(AuthenticationFailureCategory.Timeout, 504)]
    public async Task FailedLogin_WithOversizedIdentity_PreservesFailureAudit(AuthenticationFailureCategory category, int status)
    {
        var audit = new ValidatingAudit();
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditEventService>(); services.AddSingleton<IAuditEventService>(audit);
            services.RemoveAll<IAuthenticationService>(); services.AddSingleton<IAuthenticationService>(new FailedAuthentication(category));
        }));
        using var client = configured.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = new string('a', 300) + "@lab.local", password = "synthetic" });
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Empty(audit.Failures);
        var recorded = Assert.Single(audit.Events);
        Assert.False(recorded.Success);
        Assert.Null(recorded.Username);
        Assert.Null(recorded.Subject);
        Assert.Equal(category == AuthenticationFailureCategory.InvalidCredentials ? AuditEventTypes.LoginFailure : AuditEventTypes.LdapFailure, recorded.EventTypeCode);
        using var details = JsonDocument.Parse(recorded.DetailsJson!);
        Assert.Equal(category.ToString(), details.RootElement.GetProperty("failureCategory").GetString());
        Assert.True(details.RootElement.GetProperty("identityOmitted").GetBoolean());
        Assert.Equal(0, factory.ValidationCalls);
    }

    private sealed class FailedAuthentication(AuthenticationFailureCategory category) : IAuthenticationService
    {
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
            => Task.FromResult(AuthenticationResult.Failed(new(category, DirectoryFailureStage.UserBind, DirectoryFailureReason.UnexpectedFailure)));
    }

    [Theory]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(1024)]
    public async Task LoginAndDeniedRequest_ProduceValidAuditWithoutTruncatingIdentity(int length)
    {
        var audit = new ValidatingAudit();
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditEventService>();
            services.AddSingleton<IAuditEventService>(audit);
        }));
        using var client = configured.CreateClient();
        var username = new string('a', length - "@lab.local".Length) + "@lab.local";
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password = "synthetic" });
        Assert.Equal(200, (int)response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.Empty(audit.Failures);
        var login = Assert.Single(audit.Events);
        Assert.Equal(AuditEventTypes.LoginSuccess, login.EventTypeCode);
        Assert.Equal(length <= 256 ? username : null, login.Username);
        Assert.Equal(login.Username, login.Subject);
        if (length > 256)
        {
            using var details = JsonDocument.Parse(login.DetailsJson!);
            Assert.True(details.RootElement.GetProperty("identityOmitted").GetBoolean());
        }

        using var scope = configured.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var deniedToken = await issuer.IssueAsync(new TokenIssuanceRequest { Subject = username, Roles = ["Operator"], Scopes = [] });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", deniedToken.AccessToken);
        using var denied = await client.GetAsync("/api/v1/protected");
        Assert.Equal(403, (int)denied.StatusCode);
        Assert.Empty(audit.Failures);
        var deniedAudit = Assert.Single(audit.Events, value => value.EventTypeCode == AuditEventTypes.AccessDenied);
        Assert.Equal(length <= 256 ? username : null, deniedAudit.Subject);
        if (length > 256)
        {
            using var details = JsonDocument.Parse(deniedAudit.DetailsJson!);
            Assert.True(details.RootElement.GetProperty("identityOmitted").GetBoolean());
        }
        var payload = deniedToken.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        Assert.Equal(username, json.RootElement.GetProperty("sub").GetString());
    }

    [Fact]
    public async Task EmptyGuidCorrelation_IsReplacedAndDoesNotInvalidateLoginAudit()
    {
        var audit = new ValidatingAudit();
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditEventService>(); services.AddSingleton<IAuditEventService>(audit);
        }));
        using var client = configured.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.Empty.ToString("D"));
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "synthetic" });
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Empty(audit.Failures);
        var recorded = Assert.Single(audit.Events);
        Assert.NotEqual(Guid.Empty, recorded.CorrelationId);
        Assert.Equal(recorded.CorrelationId.ToString("D"), Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
    }

    [Theory]
    [InlineData(256, false)]
    [InlineData(257, true)]
    public async Task SuccessfulProtectedAccess_PreservesRequestAndBoundsAuditIdentity(int length, bool identityOmitted)
    {
        var audit = new ValidatingAudit();
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuditEventService>();
            services.AddSingleton<IAuditEventService>(audit);
        }));
        using var scope = configured.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var subject = new string('a', length);
        var token = await issuer.IssueAsync(new TokenIssuanceRequest
        {
            Subject = subject,
            Roles = ["Reader"],
            Scopes = []
        });
        using var client = configured.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Empty(audit.Failures);
        var recorded = Assert.Single(audit.Events, value => value.EventTypeCode == AuditEventTypes.AccessGranted);
        Assert.Equal(identityOmitted, recorded.Username is null && recorded.Subject is null);
        if (identityOmitted)
        {
            using var details = JsonDocument.Parse(recorded.DetailsJson!);
            Assert.True(details.RootElement.GetProperty("identityOmitted").GetBoolean());
        }
    }

    private sealed class ValidatingAudit : IAuditEventService
    {
        public List<string> Failures { get; } = [];
        public List<AuditEvent> Events { get; } = [];
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            // Same pre-SQL validator as production; retain errors outside swallowed controller exceptions.
            var failures = AuditEventValidator.Validate(auditEvent);
            Failures.AddRange(failures);
            if (failures.Count > 0) throw new ArgumentException("Audit rejected.");
            Events.Add(auditEvent);
            return Task.FromResult<long?>(1);
        }
    }
}
