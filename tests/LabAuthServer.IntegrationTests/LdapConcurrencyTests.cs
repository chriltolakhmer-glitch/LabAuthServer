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
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapConcurrencyTests
{
    public static IEnumerable<object[]> Outcomes()
    {
        foreach (var group in new[] { false, true })
        foreach (var category in new[] { AuthenticationFailureCategory.InvalidRequest, AuthenticationFailureCategory.InvalidCredentials,
                     AuthenticationFailureCategory.DirectoryUnavailable, AuthenticationFailureCategory.Timeout,
                     AuthenticationFailureCategory.ProtocolFailure, AuthenticationFailureCategory.Configuration, AuthenticationFailureCategory.Unexpected })
            yield return [category.ToString(), group];
        foreach (var outcome in new[] { "success", "empty", "role-throw", "token-throw", "auth-throw", "group-throw" })
            yield return [outcome, false];
    }

    [Theory]
    [MemberData(nameof(Outcomes))]
    public async Task EveryOutcome_ReleasesPermitBeforePostLdapWork_AndAllowsNextRequest(string outcome, bool group)
    {
        using var gate = Gate();
        var scenario = new Scenario(gate) { Outcome = outcome, FailGroups = group };
        // Repetition catches a path that leaks one permit or accidentally releases it twice.
        for (var iteration = 0; iteration < 3; iteration++)
        {
            var controller = Controller(scenario);
            if (outcome == "auth-throw")
                await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Login(Request(), default));
            else
            {
                var result = Assert.IsAssignableFrom<ObjectResult>(await controller.Login(Request(), default));
                var expected = outcome switch
                {
                    "success" => 200, "InvalidRequest" => 400, "InvalidCredentials" => 401,
                    "DirectoryUnavailable" or "ProtocolFailure" => 503, "Timeout" => 504, _ => 500
                };
                Assert.Equal(expected, result.StatusCode);
                Assert.DoesNotContain("SENSITIVE", JsonSerializer.Serialize(result.Value));
                if (Enum.TryParse<AuthenticationFailureCategory>(outcome, out _))
                {
                    Assert.Equal(0, scenario.MappingCalls);
                    Assert.Equal(0, scenario.TokenCalls);
                }
            }
            await AssertAvailable(gate);
        }
        Assert.False(scenario.ReleaseProbeFailed);
        Assert.Equal(outcome == "auth-throw" ? 0 : 3, scenario.Audits.Count);
        Assert.All(scenario.Logs, entry => { Assert.DoesNotContain("SENSITIVE", entry.Text); Assert.Null(entry.Exception); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaitingLogin_UsesExistingOriginAndSafeAudit_WithoutStartingLdap(bool deadline)
    {
        using var gate = Gate();
        using var holder = new AuthenticationOperation(TimeSpan.FromSeconds(60));
        using var held = await gate.AcquireAsync(holder);
        using var caller = new CancellationTokenSource();
        var scenario = new Scenario(gate) { CheckRelease = false };
        var controller = Controller(scenario, caller.Token);
        var login = controller.Login(Request(), caller.Token);
        Assert.False(login.IsCompleted);
        Assert.Equal(0, scenario.AuthCalls);
        if (deadline) scenario.Clock.Advance(); else caller.Cancel();
        var result = Assert.IsType<ObjectResult>(await login.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(deadline ? 504 : 499, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(deadline ? "Authentication request timed out." : "Authentication request was cancelled.", problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("correlationId"));
        var audit = Assert.Single(scenario.Audits);
        Assert.Equal(deadline ? AuditEventTypes.LdapFailure : AuditEventTypes.LoginFailure, audit.EventTypeCode);
        using var details = JsonDocument.Parse(audit.DetailsJson!);
        Assert.Equal("ConcurrencyWait", details.RootElement.GetProperty("stage").GetString());
        Assert.Equal(deadline ? "Timeout" : "Cancelled", details.RootElement.GetProperty("failureCategory").GetString());
        Assert.Equal(JsonValueKind.Null, details.RootElement.GetProperty("diagnosticCode").ValueKind);
        Assert.Equal(0, scenario.AuthCalls);
        Assert.Equal(0, scenario.GroupCalls);
        Assert.Equal(0, scenario.MappingCalls);
        Assert.Equal(0, scenario.TokenCalls);
        held.Dispose();
        await AssertAvailable(gate);
        Assert.Equal(0, scenario.AuthCalls);
    }

    [Fact]
    public async Task WaitingLogin_ProceedsWhenHolderFinishesWithinBudget()
    {
        using var gate = Gate();
        using var holder = new AuthenticationOperation(TimeSpan.FromSeconds(60));
        using var held = await gate.AcquireAsync(holder);
        var scenario = new Scenario(gate);
        var login = Controller(scenario).Login(Request(), default);
        Assert.False(login.IsCompleted);
        Assert.Equal(0, scenario.AuthCalls);
        held.Dispose();
        Assert.Equal(200, Assert.IsType<OkObjectResult>(await login.WaitAsync(TimeSpan.FromSeconds(10))).StatusCode);
        Assert.Equal(1, scenario.AuthCalls);
        Assert.Equal(1, scenario.GroupCalls);
        Assert.Equal(1, scenario.TokenCalls);
        await AssertAvailable(gate);
    }

    [Theory]
    [InlineData("UserSearch", false)]
    [InlineData("UserSearch", true)]
    [InlineData("UserBind", false)]
    [InlineData("UserBind", true)]
    [InlineData("GroupSearch", false)]
    [InlineData("GroupSearch", true)]
    [InlineData("Dispose", false)]
    [InlineData("Dispose", true)]
    public async Task NativeWorkAndCleanup_KeepPermitAfterInterruption_UntilActualCompletion(string stage, bool deadline)
    {
        using var gate = Gate();
        using var caller = new CancellationTokenSource();
        var scenario = new Scenario(gate) { CheckRelease = false };
        using var native = new BlockingConnections(stage);
        var options = Options.Create(Settings());
        var authentication = new LdapAuthenticationService(options, new LdapAuthenticationClient(native), native, NullLogger<LdapAuthenticationService>.Instance);
        var groups = new LdapService(options, native, NullLogger<LdapService>.Instance, native);
        var first = Controller(scenario, caller.Token, authentication, groups).Login(Request(), caller.Token);
        Task<IActionResult>? second = null;
        try
        {
            await native.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var connections = native.Created;
            second = Controller(scenario, default, authentication, groups).Login(Request(), default);
            Assert.False(second.IsCompleted);
            if (deadline) scenario.Clock.Advance(); else caller.Cancel();
            Assert.False(first.IsCompleted);
            Assert.Equal(connections, native.Created);
            Assert.Equal(1, native.Active);
            Assert.Equal(0, scenario.MappingCalls);
            Assert.Equal(0, scenario.TokenCalls);
            if (deadline)
                Assert.Equal(504, Assert.IsType<ObjectResult>(await second.WaitAsync(TimeSpan.FromSeconds(10))).StatusCode);
            else Assert.False(second.IsCompleted);
        }
        finally { native.Release.Set(); }
        Assert.Equal(deadline ? 504 : 499, Assert.IsType<ObjectResult>(await first.WaitAsync(TimeSpan.FromSeconds(10))).StatusCode);
        Assert.Equal(deadline ? 504 : 200, Assert.IsAssignableFrom<ObjectResult>(await second!.WaitAsync(TimeSpan.FromSeconds(10))).StatusCode);
        Assert.Equal(deadline ? 0 : 1, scenario.TokenCalls);
        Assert.Equal(0, native.Active);
        Assert.Equal(native.Created, native.Disposed);
        await AssertAvailable(gate);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(32)]
    public async Task DiGate_IsOneInstanceAcrossRequestScopes_AndUsesConfiguredLimit(int limit)
    {
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<LdapOptions>(options => options.MaxConcurrentLdapOperations = limit)));
        using var first = configured.Services.CreateScope();
        using var second = configured.Services.CreateScope();
        var gate = first.ServiceProvider.GetRequiredService<ILdapConcurrencyLimiter>();
        Assert.Same(gate, second.ServiceProvider.GetRequiredService<ILdapConcurrencyLimiter>());
        var operations = new List<AuthenticationOperation>();
        var leases = new List<IDisposable>();
        try
        {
            for (var i = 0; i < limit; i++) { var operation = new AuthenticationOperation(TimeSpan.FromSeconds(60)); operations.Add(operation); leases.Add(await gate.AcquireAsync(operation)); }
            using var cancellation = new CancellationTokenSource();
            using var waiter = new AuthenticationOperation(TimeSpan.FromSeconds(60), cancellation.Token);
            var waiting = second.ServiceProvider.GetRequiredService<ILdapConcurrencyLimiter>().AcquireAsync(waiter);
            Assert.False(waiting.IsCompleted);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        }
        finally { foreach (var lease in leases) lease.Dispose(); foreach (var operation in operations) operation.Dispose(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(33)]
    [InlineData(int.MaxValue)]
    public async Task InvalidLimit_FailsApplicationStartup(int limit)
    {
        // Exercise the production ValidateOnStart registration with a directly owned host.
        // Deferred WebApplicationFactory failure teardown can race and mask the validation exception.
        using var host = new HostBuilder().ConfigureServices((context, services) =>
        {
            services.AddActiveDirectoryOptions(new ConfigurationBuilder().Build(), context.HostingEnvironment);
            services.Configure<LdapOptions>(options =>
            {
                options.Domain = "lab.local";
                options.Host = "directory.example.test";
                options.BaseDn = "DC=lab,DC=local";
                options.UserSearchBaseDn = "DC=lab,DC=local";
                options.ServiceAccountUsername = "service@lab.local";
                options.MaxConcurrentLdapOperations = limit;
            });
        }).Build();
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Equal(typeof(LdapOptions), exception.OptionsType);
        Assert.Contains("The Active Directory configuration is invalid.", exception.Failures);
    }

    [Fact]
    public async Task HttpContention_Preserves504CorrelationHeaders_AndExisting429Admission()
    {
        using var gate = Gate();
        var observed = new ObservedGate(gate);
        var scenario = new Scenario(gate) { CheckRelease = false };
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILdapConcurrencyLimiter>(); services.AddSingleton<ILdapConcurrencyLimiter>(observed);
            services.RemoveAll<IAuthenticationService>(); services.AddSingleton<IAuthenticationService>(scenario);
            services.RemoveAll<ILdapService>(); services.AddSingleton<ILdapService>(scenario);
            services.RemoveAll<IAuditEventService>(); services.AddSingleton<IAuditEventService>(scenario);
            services.AddSingleton<TimeProvider>(scenario.Clock);
            services.Configure<LdapOptions>(options => options.AuthenticationTimeout = TimeSpan.FromSeconds(5));
        }));
        using var holder = new AuthenticationOperation(TimeSpan.FromSeconds(60));
        using var held = await gate.AcquireAsync(holder);
        using var client = configured.CreateClient();
        var correlation = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        var pending = Enumerable.Range(0, 10).Select(_ => client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "synthetic" })).ToArray();
        await observed.TenWaiters.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.All(pending, task => Assert.False(task.IsCompleted));
        using var excess = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@lab.local", password = "synthetic" });
        Assert.Equal(429, (int)excess.StatusCode);
        Assert.Equal(10, observed.Attempts);
        Assert.Equal(0, scenario.AuthCalls);
        scenario.Clock.Advance();
        var responses = await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(10));
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(504, (int)response.StatusCode);
                Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
                Assert.True(response.Headers.CacheControl!.NoStore);
                Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
                Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
                var text = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("SENSITIVE", text);
                using var body = JsonDocument.Parse(text);
                Assert.Equal(correlation, body.RootElement.GetProperty("correlationId").GetString());
            }
        }
        Assert.Equal(0, scenario.AuthCalls);
        Assert.Equal(10, scenario.Audits.Count);
        held.Dispose();
        await AssertAvailable(gate);
    }

    private static LdapOptions Settings() => new() { MaxConcurrentLdapOperations = 1, AuthenticationTimeout = TimeSpan.FromSeconds(5),
        Domain = "lab.local", Host = "directory.example.test", BaseDn = "DC=lab,DC=local", UserSearchBaseDn = "DC=lab,DC=local", ServiceAccountUsername = "service@lab.local" };
    private static LdapConcurrencyLimiter Gate() => new(Options.Create(Settings()));
    private static LoginRequest Request() => new() { Username = "reader@lab.local", Password = "synthetic" };
    private static AuthController Controller(Scenario scenario, CancellationToken caller = default, IAuthenticationService? authentication = null, ILdapService? groups = null)
    {
        var controller = new AuthController(authentication ?? scenario, groups ?? scenario, scenario, scenario, scenario.Gate,
            scenario, scenario, Options.Create(Settings()), scenario.Clock) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Scheme = "https";
        controller.HttpContext.RequestAborted = caller;
        controller.HttpContext.Items[CorrelationContext.ItemKey] = new CorrelationContext(Guid.NewGuid(), "test-request");
        return controller;
    }
    private static async Task AssertAvailable(ILdapConcurrencyLimiter gate)
    {
        using var cancel = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), cancel.Token);
        var acquisition = gate.AcquireAsync(operation);
        if (!acquisition.IsCompletedSuccessfully)
        {
            cancel.Cancel();
            try { (await acquisition).Dispose(); } catch (OperationCanceledException) { }
            Assert.Fail("LDAP permit remained held outside protected work.");
        }
        using var lease = await acquisition;
        Assert.True(lease.IsSuccess);
    }

    private sealed class Scenario(ILdapConcurrencyLimiter gate) : IAuthenticationService, ILdapService, IAuthorizationMappingService, ITokenService, IAuditEventService, ILogger<AuthController>
    {
        public ILdapConcurrencyLimiter Gate { get; } = gate;
        public ManualClock Clock { get; } = new();
        public string Outcome { get; init; } = "success";
        public bool FailGroups { get; init; }
        public bool CheckRelease { get; init; } = true;
        public int AuthCalls, GroupCalls, MappingCalls, TokenCalls;
        public bool ReleaseProbeFailed;
        public ConcurrentQueue<AuditEvent> Audits { get; } = new();
        public ConcurrentQueue<(string Text, Exception? Exception)> Logs { get; } = new();
        private DirectoryFailure? Failure => Enum.TryParse<AuthenticationFailureCategory>(Outcome, out var category)
            ? new(category, FailGroups ? DirectoryFailureStage.GroupSearch : DirectoryFailureStage.UserBind, DirectoryFailureReason.UnexpectedFailure) : null;
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            AuthCalls++;
            if (Outcome == "auth-throw") throw new InvalidOperationException("SENSITIVE-auth");
            return Task.FromResult(!FailGroups && Failure is { } failure ? AuthenticationResult.Failed(failure) : AuthenticationResult.Succeeded(username));
        }
        public Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            GroupCalls++;
            if (Outcome == "group-throw") throw new InvalidOperationException("SENSITIVE-groups");
            return Task.FromResult(FailGroups && Failure is { } failure ? GroupLookupResult.Failed(failure) : GroupLookupResult.Succeeded(Outcome == "empty" ? [] : ["GG-APP-USER"]));
        }
        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async Task<AuthorizationMappingResult> MapGroupsToRolesAsync(IReadOnlyCollection<string> groupIdentifiers, CancellationToken cancellationToken = default)
        {
            MappingCalls++;
            await CheckReleasedAsync();
            if (Outcome == "role-throw") throw new InvalidOperationException("SENSITIVE-role");
            return new() { Roles = groupIdentifiers.Count == 0 ? [] : ["Reader"] };
        }
        public async Task<TokenResponse> IssueAsync(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
        {
            TokenCalls++;
            await CheckReleasedAsync();
            if (Outcome == "token-throw" || request.Roles.Count == 0) throw new InvalidOperationException("SENSITIVE-token");
            return new() { AccessToken = "synthetic-token", TokenType = "Bearer", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) };
        }
        public async Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await CheckReleasedAsync();
            Audits.Enqueue(auditEvent);
            return 1;
        }
        private async Task CheckReleasedAsync()
        {
            if (!CheckRelease) return;
            try { await AssertAvailable(Gate); }
            catch { ReleaseProbeFailed = true; throw; }
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Logs.Enqueue((formatter(state, exception), exception));
    }

    private sealed class BlockingConnections(string target) : ILdapConnectionFactory, ILdapServiceAccountCredentialProvider, IDisposable
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new();
        public int Created, Active, Disposed;
        private int _blocked;
        private void Visit(string stage)
        {
            if (stage == target && Interlocked.CompareExchange(ref _blocked, 1, 0) == 0)
            {
                Entered.SetResult();
                Assert.True(Release.Wait(TimeSpan.FromSeconds(15)), "Test native operation was not released.");
            }
        }
        public ILdapConnection Create(LdapOptions options) { Interlocked.Increment(ref Created); Assert.Equal(1, Interlocked.Increment(ref Active)); return new Connection(this); }
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default) => Task.FromResult("synthetic-service");
        public void Dispose() { Release.Set(); Release.Dispose(); }
        private sealed class Connection(BlockingConnections owner) : ILdapConnection
        {
            private int _binds;
            public void ConfigureSession(bool useLdaps) { }
            public void Bind(NetworkCredential credential) { if (++_binds == 2) owner.Visit("UserBind"); }
            public LdapSearchResult Search(SearchRequest request)
            {
                var group = request.Attributes.Contains("memberOf");
                owner.Visit(group ? "GroupSearch" : "UserSearch");
                const string dn = "CN=Reader,DC=lab,DC=local";
                var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
                { ["distinguishedName"] = [dn], ["userPrincipalName"] = ["reader@lab.local"], ["userAccountControl"] = ["512"] };
                if (group) attributes["memberOf"] = ["CN=GG-APP-USER,DC=lab,DC=local"];
                return new(ResultCode.Success, [new(dn, attributes)]);
            }
            public void Dispose() { owner.Visit("Dispose"); Interlocked.Decrement(ref owner.Active); Interlocked.Increment(ref owner.Disposed); }
        }
    }

    private sealed class ObservedGate(ILdapConcurrencyLimiter inner) : ILdapConcurrencyLimiter
    {
        public TaskCompletionSource TenWaiters { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Attempts;
        public Task<LdapAdmissionResult> AcquireAsync(AuthenticationOperation operation)
        {
            var task = inner.AcquireAsync(operation);
            if (Interlocked.Increment(ref Attempts) == 10) TenWaiters.TrySetResult();
            return task;
        }
    }
    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;
        private readonly ConcurrentBag<Timer> _timers = [];
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        { var timer = new Timer(callback, state, GetTimestamp() + dueTime.Ticks); _timers.Add(timer); return timer; }
        public void Advance()
        { Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(5).Ticks); foreach (var timer in _timers) if (!timer.Disposed && GetTimestamp() >= timer.Due) timer.Callback(timer.State); }
        private sealed class Timer(TimerCallback callback, object? state, long due) : ITimer
        {
            public TimerCallback Callback { get; } = callback;
            public object? State { get; } = state;
            public long Due { get; } = due;
            public bool Disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() => Disposed = true;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
