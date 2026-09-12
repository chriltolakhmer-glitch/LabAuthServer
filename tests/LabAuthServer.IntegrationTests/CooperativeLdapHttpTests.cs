using System.Collections.Concurrent;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LabAuthServer.Api.Controllers;
using LabAuthServer.Api.Extensions;
using LabAuthServer.Api.Requests;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Security;
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class CooperativeLdapHttpTests
{
    public static IEnumerable<object[]> Deadlines()
    {
        foreach (var stage in new[] { "credentials", "identity-setup", "service-bind", "user-search", "user-bind", "group-bind", "group-search", "mapping", "token" })
        foreach (var lateResult in new[] { "success", "49", "timeout", "cancel" })
            yield return [stage, lateResult];
    }

    [Theory]
    [MemberData(nameof(Deadlines))]
    public async Task DeadlineThroughActualServices_ReturnsSafe504AndStopsPipeline(string stage, string lateResult)
    {
        var scenario = new Scenario(stage, lateResult);
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        var correlation = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "SENSITIVE-user" });
        Assert.Equal(504, (int)response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(text);
        Assert.Equal("Authentication request timed out.", body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(correlation, body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.DoesNotContain("SENSITIVE", text);
        Assert.DoesNotContain("StackTrace", text);
        VerifyFailure(scenario, stage, deadline: true);
    }

    [Theory]
    [InlineData("credentials")]
    [InlineData("identity-setup")]
    [InlineData("service-bind")]
    [InlineData("user-search")]
    [InlineData("user-bind")]
    [InlineData("group-bind")]
    [InlineData("group-search")]
    [InlineData("mapping")]
    [InlineData("token")]
    public async Task CallerCancellation_Produces499WhenWritable_AndAuditRemainsBestEffort(string stage)
    {
        using var caller = new CancellationTokenSource();
        var scenario = new Scenario(stage, "cancel") { Caller = caller };
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var scope = configured.Services.CreateScope();
        var services = scope.ServiceProvider;
        var controller = new AuthController(services.GetRequiredService<IAuthenticationService>(), services.GetRequiredService<ILdapService>(),
            scenario, services.GetRequiredService<ITokenService>(), services.GetRequiredService<ILdapConcurrencyLimiter>(), services.GetRequiredService<ILogger<AuthController>>(), scenario,
            Options.Create(Settings()), scenario.Clock)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Scheme = "https";
        controller.HttpContext.RequestAborted = caller.Token;
        var result = Assert.IsType<ObjectResult>(await controller.Login(new LoginRequest { Username = "reader@lab.local", Password = "SENSITIVE-user" }, caller.Token));
        Assert.Equal(499, result.StatusCode);
        Assert.Equal("Authentication request was cancelled.", Assert.IsType<ProblemDetails>(result.Value).Detail);
        Assert.True(scenario.AuditTokenCancelled);
        Assert.False(scenario.Persisted);
        VerifyFailure(scenario, stage, deadline: false);
    }

    [Theory]
    [InlineData(false, 200)]
    [InlineData(true, 500)]
    public async Task ComfortableDeadline_PreservesSuccessAndNoRolePolicy(bool empty, int status)
    {
        var scenario = new Scenario("none", "success") { Empty = empty };
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "synthetic" });
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(1, scenario.MappingCalls);
        Assert.Equal(1, scenario.TokenCalls);
        Assert.Equal(status == 200, Assert.Single(scenario.Audits).Success);
        if (status == 200)
        {
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
            client.DefaultRequestHeaders.Authorization = new("Bearer", token!.AccessToken);
            using var protectedResponse = await client.GetAsync("/api/v1/protected");
            Assert.Equal(200, (int)protectedResponse.StatusCode);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void InvalidAuthenticationDeadline_FailsStartup(int seconds)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LdapOptions.SectionName}:AuthenticationTimeout"] = seconds.ToString()
            })
            .Build();
        using var services = new ServiceCollection()
            .AddActiveDirectoryOptions(configuration, new TestHostEnvironment())
            .BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => _ = services.GetRequiredService<IOptions<LdapOptions>>().Value);

        Assert.Equal(["The Active Directory configuration is invalid."], exception.Failures);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = typeof(CooperativeLdapHttpTests).Assembly.GetName().Name!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ControllerRejectsLateSuccessEvenWhenAnImplementationIgnoresItsToken(bool groups)
    {
        var scenario = new Scenario("none", "success");
        using var factory = new JwtSizeApiFactory();
        using var configured = Configure(factory, scenario);
        using var late = configured.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            if (groups) { services.RemoveAll<ILdapService>(); services.AddSingleton<ILdapService>(new LateGroups(scenario)); }
            else { services.RemoveAll<IAuthenticationService>(); services.AddSingleton<IAuthenticationService>(new LateIdentity(scenario)); }
        }));
        using var client = late.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "synthetic" });
        Assert.Equal(504, (int)response.StatusCode);
        Assert.Equal(0, scenario.MappingCalls);
        Assert.Equal(0, scenario.TokenCalls);
        Assert.False(Assert.Single(scenario.Audits).Success);
    }

    private sealed class LateIdentity(Scenario scenario) : IAuthenticationService
    {
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            operation!.EnterStage(DirectoryFailureStage.UserBind);
            scenario.Clock.Advance();
            return Task.FromResult(AuthenticationResult.Succeeded(username));
        }
    }
    private sealed class LateGroups(Scenario scenario) : ILdapService
    {
        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            scenario.Clock.Advance();
            return Task.FromResult(GroupLookupResult.Succeeded(["GG-APP-USER"]));
        }
    }

    private static void VerifyFailure(Scenario scenario, string stage, bool deadline)
    {
        var audit = Assert.Single(scenario.Audits);
        Assert.False(audit.Success);
        Assert.Equal(deadline ? AuditEventTypes.LdapFailure : AuditEventTypes.LoginFailure, audit.EventTypeCode);
        Assert.Equal<short?>(deadline ? (short)504 : (short)499, audit.StatusCode);
        using var details = JsonDocument.Parse(audit.DetailsJson!);
        Assert.Equal(deadline ? "Timeout" : "Cancelled", details.RootElement.GetProperty("failureCategory").GetString());
        Assert.Equal(deadline ? "AuthenticationDeadlineExceeded" : "CallerCancelled", details.RootElement.GetProperty("reason").GetString());
        Assert.Equal(stage switch
        {
            "credentials" => "CredentialLoading", "identity-setup" => "ConnectionSetup", "service-bind" or "group-bind" => "ServiceBind",
            "user-search" => "UserSearch", "user-bind" => "UserBind", "group-search" => "GroupSearch", "mapping" => "RoleMapping", _ => "TokenIssuance"
        }, details.RootElement.GetProperty("stage").GetString());
        Assert.Equal("None", details.RootElement.GetProperty("diagnosticSource").GetString());
        Assert.Equal(JsonValueKind.Null, details.RootElement.GetProperty("diagnosticCode").ValueKind);
        Assert.Equal(stage is "mapping" or "token" ? 1 : 0, scenario.MappingCalls);
        Assert.Equal(stage == "token" ? 1 : 0, scenario.TokenCalls);
        Assert.Equal(scenario.Connections, scenario.Disposals);
        Assert.All(scenario.Logs, item => { Assert.DoesNotContain("SENSITIVE", item.Text); Assert.Null(item.Exception); });
        Assert.DoesNotContain("SENSITIVE", audit.DetailsJson);
    }

    private static LdapOptions Settings() => new()
    {
        Domain = "lab.local", Host = "directory.example.test", BaseDn = "DC=lab,DC=local", UserSearchBaseDn = "DC=lab,DC=local",
        ServiceAccountUsername = "service@lab.local", AuthenticationTimeout = TimeSpan.FromSeconds(5)
    };

    private static WebApplicationFactory<Program> Configure(JwtSizeApiFactory factory, Scenario scenario)
        => factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>(); services.AddScoped<IAuthenticationService, LdapAuthenticationService>();
            services.RemoveAll<ILdapService>(); services.AddScoped<ILdapService, LdapService>();
            services.RemoveAll<ILdapAuthenticationClient>(); services.AddScoped<ILdapAuthenticationClient, LdapAuthenticationClient>();
            services.RemoveAll<ILdapConnectionFactory>(); services.AddSingleton<ILdapConnectionFactory>(scenario);
            services.RemoveAll<ILdapServiceAccountCredentialProvider>(); services.AddSingleton<ILdapServiceAccountCredentialProvider>(scenario);
            services.RemoveAll<IAuthorizationMappingService>(); services.AddSingleton<IAuthorizationMappingService>(scenario);
            services.RemoveAll<IAuditEventService>(); services.AddSingleton<IAuditEventService>(scenario);
            services.AddSingleton<TimeProvider>(scenario.Clock);
            services.Configure<LdapOptions>(options => { options.Domain = "lab.local"; options.ServiceAccountUsername = "service@lab.local"; options.AuthenticationTimeout = TimeSpan.FromSeconds(5); });
            services.RemoveAll<ITokenService>();
            services.AddScoped<ITokenService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<TokenOptions>>().Value;
                return new ObservedTokens(scenario, new TokenService(options.Issuer, options.Audience, options.AccessTokenLifetime,
                    options.MaximumClaimSize, options.MaximumTokenSize, provider.GetRequiredService<ITokenSigningService>()));
            });
            services.AddSingleton<ILoggerProvider>(new LogProvider(scenario));
        }));

    private sealed class Scenario(string target, string lateResult) : ILdapConnectionFactory, ILdapServiceAccountCredentialProvider, IAuthorizationMappingService, IAuditEventService
    {
        public ManualClock Clock { get; } = new();
        public CancellationTokenSource? Caller { get; init; }
        public bool Empty { get; init; }
        public int Connections, Disposals, MappingCalls, TokenCalls;
        public bool AuditTokenCancelled, Persisted;
        public List<AuditEvent> Audits { get; } = [];
        public ConcurrentQueue<(string Text, Exception? Exception)> Logs { get; } = new();
        private bool _interrupted;
        public void Visit(string stage)
        {
            Assert.False(_interrupted, "Subsequent work started after interruption.");
            if (stage != target) return;
            _interrupted = true;
            if (Caller is not null) Caller.Cancel(); else Clock.Advance();
            switch (lateResult)
            {
                case "49": throw new LdapException(49, "SENSITIVE-provider");
                case "timeout": throw new TimeoutException("SENSITIVE-timeout");
                case "cancel": throw new OperationCanceledException("SENSITIVE-cancellation");
            }
        }
        public ILdapConnection Create(LdapOptions options) => new Connection(this, ++Connections == 2);
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default) { Visit("credentials"); return Task.FromResult("synthetic-service"); }
        public Task<AuthorizationMappingResult> MapGroupsToRolesAsync(IReadOnlyCollection<string> groupIdentifiers, CancellationToken cancellationToken = default)
        {
            MappingCalls++; Visit("mapping");
            return Task.FromResult(new AuthorizationMappingResult { Roles = Empty ? [] : ["Reader"] });
        }
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Audits.Add(auditEvent);
            AuditTokenCancelled = cancellationToken.IsCancellationRequested;
            if (AuditTokenCancelled) return Task.FromCanceled<long?>(cancellationToken);
            Persisted = true;
            return Task.FromResult<long?>(1);
        }
        private sealed class Connection(Scenario owner, bool group) : ILdapConnection
        {
            private int _binds;
            public void ConfigureSession(bool useLdaps) => owner.Visit(group ? "group-setup" : "identity-setup");
            public void Bind(NetworkCredential credential) => owner.Visit(group ? "group-bind" : ++_binds == 1 ? "service-bind" : "user-bind");
            public LdapSearchResult Search(SearchRequest request)
            {
                owner.Visit(group ? "group-search" : "user-search");
                const string dn = "CN=Reader,DC=lab,DC=local";
                var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["distinguishedName"] = [dn], ["userPrincipalName"] = ["reader@lab.local"], ["userAccountControl"] = ["512"]
                };
                if (group) attributes["memberOf"] = owner.Empty ? [] : ["CN=GG-APP-USER,DC=lab,DC=local"];
                return new(ResultCode.Success, [new(dn, attributes)]);
            }
            public void Dispose() => owner.Disposals++;
        }
    }

    private sealed class ObservedTokens(Scenario scenario, ITokenService inner) : ITokenService
    {
        public Task<TokenResponse> IssueAsync(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
        {
            scenario.TokenCalls++; scenario.Visit("token");
            return inner.IssueAsync(request, cancellationToken);
        }
    }
    private sealed class LogProvider(Scenario scenario) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLog(scenario);
        public void Dispose() { }
    }
    private sealed class CapturingLog(Scenario scenario) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => scenario.Logs.Enqueue((formatter(state, exception), exception));
    }
    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        // Deliberately do not deliver timer callbacks: production checkpoints must detect elapsed time too.
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => new InertTimer();
        public void Advance() => Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(5).Ticks);
        private sealed class InertTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
