using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using LabAuthServer.Api.Controllers;
using LabAuthServer.Api.Extensions;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapPendingWaiterHttpTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(129)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task InvalidPendingCap_FailsProductionStartupValidation(int cap)
    {
        using var host = new HostBuilder().ConfigureServices((context, services) =>
        {
            services.AddActiveDirectoryOptions(new ConfigurationBuilder().Build(), context.HostingEnvironment);
            services.Configure<LdapOptions>(options =>
            {
                options.Domain = "lab.local"; options.Host = "directory.example.test";
                options.BaseDn = "DC=lab,DC=local"; options.UserSearchBaseDn = "DC=lab,DC=local";
                options.ServiceAccountUsername = "service@lab.local"; options.MaxPendingLdapWaiters = cap;
            });
        }).Build();
        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
        Assert.Equal(typeof(LdapOptions), error.OptionsType);
        Assert.Contains("The Active Directory configuration is invalid.", error.Failures);
    }

    [Fact]
    public async Task CombinedIngressActivePendingDeadline_RejectsSafelyBeforeAllDownstreamWork()
    {
        var scenario = new Scenario();
        var clock = new ManualClock();
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Configure<LdapOptions>(options => options.MaxPendingLdapWaiters = 2);
            services.RemoveAll<ILdapConcurrencyLimiter>();
            services.AddSingleton<ILdapConcurrencyLimiter>(provider =>
                new ObservedGate(new LdapConcurrencyLimiter(provider.GetRequiredService<IOptions<LdapOptions>>())));
            services.RemoveAll<IAuthenticationService>(); services.AddSingleton<IAuthenticationService>(scenario);
            services.RemoveAll<ILdapService>(); services.AddSingleton<ILdapService>(scenario);
            services.RemoveAll<IAuthorizationMappingService>(); services.AddSingleton<IAuthorizationMappingService>(scenario);
            services.RemoveAll<ITokenService>(); services.AddSingleton<ITokenService>(scenario);
            services.RemoveAll<IAuditEventService>(); services.AddSingleton<IAuditEventService>(scenario);
            services.AddSingleton<ILogger<AuthController>>(scenario);
            services.AddSingleton<TimeProvider>(clock);
        }));
        var options = configured.Services.GetRequiredService<IOptions<LdapOptions>>().Value;
        Assert.Equal(4, options.MaxConcurrentLdapOperations);
        Assert.Equal(TimeSpan.FromSeconds(30), options.AuthenticationTimeout);
        var gate = Assert.IsType<ObservedGate>(configured.Services.GetRequiredService<ILdapConcurrencyLimiter>());
        using var scope = configured.Services.CreateScope();
        Assert.Same(gate, scope.ServiceProvider.GetRequiredService<ILdapConcurrencyLimiter>());
        var holders = Enumerable.Range(0, 4).Select(_ => new AuthenticationOperation(TimeSpan.FromSeconds(60))).ToArray();
        var leases = new List<LdapAdmissionResult>();
        using var client = configured.CreateClient();
        var correlation = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        Task<HttpResponseMessage>[] pending = [];
        try
        {
            foreach (var holder in holders)
            {
                var lease = await gate.Inner.AcquireAsync(holder);
                Assert.True(lease.IsSuccess); leases.Add(lease);
            }
            pending = Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/login",
                new { username = "reader@lab.local", password = "synthetic" })).ToArray();
            await gate.TwoPending.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.All(pending, task => Assert.False(task.IsCompleted));
            // All remaining admissions in the fixed window hit the cap, not LDAP.
            for (var attempt = 0; attempt < 8; attempt++)
            {
                using var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                    new { username = "reader@lab.local", password = "synthetic" });
                Assert.Equal(503, (int)response.StatusCode);
                Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
                Assert.True(response.Headers.CacheControl!.NoStore);
                Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
                Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
                var responseText = await response.Content.ReadAsStringAsync();
                using var body = JsonDocument.Parse(responseText);
                Assert.Equal("Authentication service unavailable.", body.RootElement.GetProperty("detail").GetString());
                foreach (var forbidden in new[] { "semaphore", "pending", "capacity", "queue", "SENSITIVE", "synthetic", "lab.local", "stack" })
                    Assert.DoesNotContain(forbidden, responseText, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Equal(10, gate.Attempts);
            Assert.Equal(8, scenario.Audits.Count);
            Assert.All(scenario.Audits, audit =>
            {
                Assert.Equal(AuditEventTypes.LdapFailure, audit.EventTypeCode);
                Assert.False(audit.Success);
                Assert.Equal((short)503, audit.StatusCode);
                Assert.Equal(Guid.Parse(correlation), audit.CorrelationId);
                using var details = JsonDocument.Parse(audit.DetailsJson!);
                var value = details.RootElement;
                Assert.Equal("ResourceExhausted", value.GetProperty("failureCategory").GetString());
                Assert.Equal("ConcurrencyWait", value.GetProperty("stage").GetString());
                Assert.Equal("PendingWaiterCapacityExceeded", value.GetProperty("reason").GetString());
                Assert.Equal("None", value.GetProperty("diagnosticSource").GetString());
                Assert.Equal(JsonValueKind.Null, value.GetProperty("diagnosticCode").ValueKind);
                Assert.Equal(new[] { "diagnosticCode", "diagnosticSource", "failureCategory", "identityOmitted", "reason", "stage" },
                    value.EnumerateObject().Select(property => property.Name).Order().ToArray());
            });
            using var rateRejected = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { username = "reader@lab.local", password = "synthetic" });
            Assert.Equal(429, (int)rateRejected.StatusCode);
            Assert.Equal(10, gate.Attempts);
            clock.Advance();
            foreach (var task in pending)
            {
                using var response = await task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(504, (int)response.StatusCode);
            }
            Assert.Equal(10, scenario.Audits.Count);
            Assert.Empty(scenario.AuditFailures);
            Assert.Equal(0, scenario.DownstreamCalls);
            Assert.All(scenario.Logs, entry =>
            {
                Assert.Null(entry.Exception);
                Assert.DoesNotContain("SENSITIVE", entry.Text);
                Assert.DoesNotContain("synthetic", entry.Text);
                Assert.DoesNotContain("configured", entry.Text, StringComparison.OrdinalIgnoreCase);
            });
            foreach (var lease in leases) lease.Dispose();
            using var next = new AuthenticationOperation(TimeSpan.FromSeconds(30));
            using var recovered = await gate.Inner.AcquireAsync(next);
            Assert.True(recovered.IsSuccess);
        }
        finally
        {
            clock.Advance();
            foreach (var lease in leases) lease.Dispose();
            foreach (var holder in holders) holder.Dispose();
            foreach (var task in pending) { using var response = await task.WaitAsync(TimeSpan.FromSeconds(10)); }
        }
    }

    private sealed class ObservedGate(LdapConcurrencyLimiter inner) : ILdapConcurrencyLimiter, IDisposable
    {
        public LdapConcurrencyLimiter Inner { get; } = inner;
        public int Attempts;
        public TaskCompletionSource TwoPending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<LdapAdmissionResult> AcquireAsync(AuthenticationOperation operation)
        {
            var result = Inner.AcquireAsync(operation);
            if (Interlocked.Increment(ref Attempts) == 2) TwoPending.TrySetResult();
            return result;
        }
        public void Dispose() => Inner.Dispose();
    }
    private sealed class Scenario : IAuthenticationService, ILdapService, IAuthorizationMappingService, ITokenService,
        IAuditEventService, ILogger<AuthController>
    {
        public int DownstreamCalls;
        public ConcurrentQueue<AuditEvent> Audits { get; } = new();
        public ConcurrentQueue<string> AuditFailures { get; } = new();
        public ConcurrentQueue<(string Text, Exception? Exception)> Logs { get; } = new();
        private Exception Unexpected() { Interlocked.Increment(ref DownstreamCalls); return new InvalidOperationException("SENSITIVE downstream work"); }
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null) => throw Unexpected();
        public Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null) => throw Unexpected();
        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<AuthorizationMappingResult> MapGroupsToRolesAsync(IReadOnlyCollection<string> groupIdentifiers, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<TokenResponse> IssueAsync(TokenIssuanceRequest request, CancellationToken cancellationToken = default) => throw Unexpected();
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            foreach (var failure in AuditEventValidator.Validate(auditEvent)) AuditFailures.Enqueue(failure);
            Audits.Enqueue(auditEvent);
            return Task.FromResult<long?>(1);
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Logs.Enqueue((formatter(state, exception), exception));
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
        { Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(30).Ticks); foreach (var timer in _timers) if (!timer.Disposed && GetTimestamp() >= timer.Due) timer.Callback(timer.State); }
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
