using System.Net.Http.Json;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.Json;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.Security;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapFailureHttpTests
{
    [Theory]
    [InlineData("service49", 503, AuditEventTypes.LdapFailure)]
    [InlineData("user49", 401, AuditEventTypes.LoginFailure)]
    [InlineData("group49", 503, AuditEventTypes.LdapFailure)]
    [InlineData("group-timeout", 504, AuditEventTypes.LdapFailure)]
    [InlineData("invalid-response", 503, AuditEventTypes.LdapFailure)]
    [InlineData("success", 200, AuditEventTypes.LoginSuccess)]
    public async Task RealClassificationPipeline_UsesConnectionSeamAndSafeAudit(string condition, int status, string eventType)
    {
        var scenario = new Scenario();
        using var factory = new JwtSizeApiFactory();
        using var observed = Configure(factory, scenario);
        using var configured = observed.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddScoped<IAuthenticationService, LdapAuthenticationService>();
            services.RemoveAll<ILdapService>();
            services.AddScoped<ILdapService, LdapService>();
            services.RemoveAll<ILdapAuthenticationClient>();
            services.AddScoped<ILdapAuthenticationClient, LdapAuthenticationClient>();
            services.RemoveAll<ILdapConnectionFactory>();
            services.AddSingleton<ILdapConnectionFactory>(new PipelineConnectionFactory(condition));
            services.RemoveAll<ILdapServiceAccountCredentialProvider>();
            services.AddSingleton<ILdapServiceAccountCredentialProvider>(new PipelineCredentialProvider());
            services.Configure<LdapOptions>(options =>
            {
                options.Domain = "lab.local";
                options.ServiceAccountUsername = "service@lab.local";
            });
        }));
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "SENSITIVE-user-secret" });
        Assert.Equal(status, (int)response.StatusCode);
        var audit = Assert.Single(scenario.Audits);
        Assert.Equal(eventType, audit.EventTypeCode);
        Assert.Equal(status == 200, audit.Success);
        Assert.Equal(status == 200 ? 1 : 0, scenario.TokenCalls);
        Assert.Equal(status == 200 ? 1 : 0, scenario.MappingCalls);
        Assert.DoesNotContain("SENSITIVE", await response.Content.ReadAsStringAsync());
        Assert.All(scenario.Logs, entry =>
        {
            Assert.DoesNotContain("SENSITIVE", entry.Text);
            Assert.Null(entry.Exception);
        });
        if (condition is "service49" or "group49")
        {
            using var details = JsonDocument.Parse(audit.DetailsJson!);
            Assert.Equal("ServiceBindRejected", details.RootElement.GetProperty("reason").GetString());
            Assert.Equal(49, details.RootElement.GetProperty("diagnosticCode").GetInt32());
            Assert.Equal(condition == "group49" ? "GroupSearch" : "ServiceBind", details.RootElement.GetProperty("stage").GetString());
        }
    }

    [Theory]
    [InlineData(AuthenticationFailureCategory.InvalidCredentials, 401, false)]
    [InlineData(AuthenticationFailureCategory.InvalidRequest, 400, false)]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, 503, false)]
    [InlineData(AuthenticationFailureCategory.Timeout, 504, false)]
    [InlineData(AuthenticationFailureCategory.Cancelled, 499, false)]
    [InlineData(AuthenticationFailureCategory.ProtocolFailure, 503, false)]
    [InlineData(AuthenticationFailureCategory.Configuration, 500, false)]
    [InlineData(AuthenticationFailureCategory.Unexpected, 500, false)]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, 503, true)]
    [InlineData(AuthenticationFailureCategory.Timeout, 504, true)]
    [InlineData(AuthenticationFailureCategory.Cancelled, 499, true)]
    [InlineData(AuthenticationFailureCategory.ProtocolFailure, 503, true)]
    [InlineData(AuthenticationFailureCategory.Configuration, 500, true)]
    [InlineData(AuthenticationFailureCategory.Unexpected, 500, true)]
    public async Task DirectoryFailure_StopsLoginAndPreservesHttpAuditContract(AuthenticationFailureCategory category, int status, bool groupFailure)
    {
        var failure = new DirectoryFailure(category,
            groupFailure ? DirectoryFailureStage.GroupSearch : DirectoryFailureStage.UserBind,
            DirectoryFailureReason.OperationFailed, 52, DirectoryDiagnosticSource.OperationResult);
        var scenario = new Scenario { Failure = failure, FailGroups = groupFailure };
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        var correlation = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "SENSITIVE-user-secret" });

        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(correlation, response.Headers.GetValues("X-Correlation-ID").Single());
        var text = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(text);
        Assert.Equal(failure.SafeMessage, body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(correlation, body.RootElement.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("SENSITIVE", text);
        Assert.DoesNotContain("diagnostic", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stage", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, scenario.MappingCalls);
        Assert.Equal(0, scenario.TokenCalls);
        Assert.Equal(groupFailure ? 1 : 0, scenario.GroupCalls);
        var audit = Assert.Single(scenario.Audits);
        Assert.Equal(category is AuthenticationFailureCategory.InvalidRequest or AuthenticationFailureCategory.InvalidCredentials or AuthenticationFailureCategory.Cancelled
            ? AuditEventTypes.LoginFailure : AuditEventTypes.LdapFailure, audit.EventTypeCode);
        Assert.False(audit.Success);
        Assert.Equal<short?>((short)status, audit.StatusCode);
        Assert.Equal(correlation, audit.CorrelationId.ToString("D"));
        Assert.Equal("reader@lab.local", audit.Username);
        Assert.Equal(audit.Username, audit.Subject);
        using var details = JsonDocument.Parse(audit.DetailsJson!);
        Assert.Equal(category.ToString(), details.RootElement.GetProperty("failureCategory").GetString());
        Assert.Equal(failure.Stage.ToString(), details.RootElement.GetProperty("stage").GetString());
        Assert.Equal(52, details.RootElement.GetProperty("diagnosticCode").GetInt32());
        Assert.DoesNotContain("SENSITIVE", audit.DetailsJson);
        Assert.Empty(AuditEventValidator.Validate(audit));
        var logs = scenario.Logs.Where(entry => entry.Text.StartsWith("Login failed:", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(logs);
        Assert.All(logs, entry =>
        {
            Assert.Null(entry.Exception);
            Assert.DoesNotContain("SENSITIVE", entry.Text);
            Assert.Contains(correlation, entry.Text);
        });
    }

    [Theory]
    [InlineData(false, false, 200)]
    [InlineData(true, false, 500)]
    [InlineData(false, true, 500)]
    public async Task CompleteLogin_OnlyAuditsSuccessAfterTokenAndRetainsNoRolePolicy(bool emptyGroups, bool tokenFailure, int status)
    {
        var scenario = new Scenario { EmptyGroups = emptyGroups, TokenFailure = tokenFailure };
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "SENSITIVE-user-secret" });
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(1, scenario.GroupCalls);
        Assert.Equal(1, scenario.MappingCalls);
        Assert.Equal(1, scenario.TokenCalls);
        Assert.Equal(new[] { "authenticate", "groups", "map", "token", "audit" }, scenario.Sequence);
        var audit = Assert.Single(scenario.Audits);
        Assert.Equal(status == 200 ? AuditEventTypes.LoginSuccess : AuditEventTypes.UnhandledException, audit.EventTypeCode);
        Assert.Equal(status == 200, audit.Success);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SENSITIVE", text);
        Assert.All(scenario.Logs, entry => Assert.DoesNotContain("SENSITIVE", entry.Text));
        if (status == 200)
        {
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(token);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
            using var protectedResponse = await client.GetAsync("/api/v1/protected");
            Assert.Equal(200, (int)protectedResponse.StatusCode);
        }
        else
        {
            using var body = JsonDocument.Parse(text);
            Assert.Equal("Token issuance failed.", body.RootElement.GetProperty("title").GetString());
        }
    }

    private static WebApplicationFactory<Program> Configure(JwtSizeApiFactory factory, Scenario scenario)
        => factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService>(scenario);
            services.RemoveAll<ILdapService>();
            services.AddSingleton<ILdapService>(scenario);
            services.RemoveAll<IAuthorizationMappingService>();
            services.AddSingleton<IAuthorizationMappingService>(scenario);
            services.RemoveAll<IAuditEventService>();
            services.AddSingleton<IAuditEventService>(scenario);
            services.RemoveAll<ITokenService>();
            services.AddScoped<ITokenService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<TokenOptions>>().Value;
                return new ObservedTokenService(scenario, new TokenService(options.Issuer, options.Audience,
                    options.AccessTokenLifetime, options.MaximumClaimSize, options.MaximumTokenSize,
                    provider.GetRequiredService<ITokenSigningService>()));
            });
            services.AddSingleton<ILoggerProvider>(new CapturingLoggerProvider(scenario));
        }));

    private sealed class Scenario : IAuthenticationService, ILdapService, IAuthorizationMappingService, IAuditEventService
    {
        public DirectoryFailure? Failure { get; init; }
        public bool FailGroups { get; init; }
        public bool EmptyGroups { get; init; }
        public bool TokenFailure { get; init; }
        public int GroupCalls;
        public int MappingCalls;
        public int TokenCalls;
        public List<string> Sequence { get; } = [];
        public List<AuditEvent> Audits { get; } = [];
        public System.Collections.Concurrent.ConcurrentQueue<(string Text, Exception? Exception)> Logs { get; } = new();
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            Sequence.Add("authenticate");
            return Task.FromResult(Failure is not null && !FailGroups ? AuthenticationResult.Failed(Failure) : AuthenticationResult.Succeeded(username));
        }
        public Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            GroupCalls++;
            Sequence.Add("groups");
            return Task.FromResult(FailGroups ? GroupLookupResult.Failed(Failure!) : GroupLookupResult.Succeeded(EmptyGroups ? [] : ["GG-APP-USER"]));
        }
        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AuthorizationMappingResult> MapGroupsToRolesAsync(IReadOnlyCollection<string> groupIdentifiers, CancellationToken cancellationToken = default)
        {
            MappingCalls++;
            Sequence.Add("map");
            return Task.FromResult(new AuthorizationMappingResult { Roles = groupIdentifiers.Count == 0 ? [] : ["Reader"] });
        }
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Sequence.Add("audit");
            Audits.Add(auditEvent);
            return Task.FromResult<long?>(1);
        }
    }

    private sealed class ObservedTokenService(Scenario scenario, ITokenService inner) : ITokenService
    {
        public Task<TokenResponse> IssueAsync(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
        {
            scenario.TokenCalls++;
            scenario.Sequence.Add("token");
            Assert.Empty(scenario.Audits);
            if (scenario.TokenFailure) throw new InvalidOperationException("SENSITIVE-stack-host-DN-filter");
            return inner.IssueAsync(request, cancellationToken);
        }
    }

    private sealed class CapturingLoggerProvider(Scenario scenario) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(scenario);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(Scenario scenario) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => scenario.Logs.Enqueue((formatter(state, exception), exception));
    }

    private sealed class PipelineCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("SENSITIVE-service-secret");
    }

    private sealed class PipelineConnectionFactory(string condition) : ILdapConnectionFactory
    {
        private int _connections;
        public ILdapConnection Create(LdapOptions options) => new Connection(condition, ++_connections == 2);

        private sealed class Connection(string condition, bool group) : ILdapConnection
        {
            private int _binds;
            public void ConfigureSession(bool useLdaps) => Assert.True(useLdaps);
            public void Bind(NetworkCredential credential)
            {
                _binds++;
                if ((condition == "service49" && !group && _binds == 1)
                    || (condition == "user49" && !group && _binds == 2)
                    || (condition == "group49" && group))
                    throw new LdapException(49, "SENSITIVE-directory-host-DN-filter");
            }
            public LdapSearchResult? Search(SearchRequest request)
            {
                if (condition == "invalid-response") return null;
                if (condition == "group-timeout" && group) throw new TimeoutException("SENSITIVE-timeout-host");
                const string dn = "CN=Reader,DC=lab,DC=local";
                var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                if (group) attributes["memberOf"] = ["CN=GG-APP-USER,DC=lab,DC=local"];
                else
                {
                    attributes["distinguishedName"] = [dn];
                    attributes["userPrincipalName"] = ["reader@lab.local"];
                    attributes["userAccountControl"] = ["512"];
                }
                return new(ResultCode.Success, [new(dn, attributes)]);
            }
            public void Dispose() { }
        }
    }
}
