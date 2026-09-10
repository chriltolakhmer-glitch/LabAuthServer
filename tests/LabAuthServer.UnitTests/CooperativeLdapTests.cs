using System.Collections;
using System.DirectoryServices.Protocols;
using System.Net;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class CooperativeLdapTests
{
    private const string User = "reader@lab.local";
    public static IEnumerable<object[]> InterruptedStages()
    {
        foreach (var group in new[] { false, true })
        foreach (var deadline in new[] { false, true })
        foreach (var stage in new[] { DirectoryFailureStage.CredentialLoading, DirectoryFailureStage.ConnectionSetup,
                     DirectoryFailureStage.ServiceBind, group ? DirectoryFailureStage.GroupSearch : DirectoryFailureStage.UserSearch,
                     DirectoryFailureStage.ResponseValidation }.Concat(group ? [] : new[] { DirectoryFailureStage.UserBind }))
        foreach (var result in new[] { "success", "49", "timeout", "cancel", "unexpected" })
            yield return [group, deadline, stage, result];
    }

    [Theory]
    [MemberData(nameof(InterruptedStages))]
    public async Task StageInterruption_OverridesLateResultAndStopsFurtherWork(bool group, bool deadline, DirectoryFailureStage stage, string result)
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        var interrupted = false;
        var stages = new List<DirectoryFailureStage>();
        void Visit(DirectoryFailureStage current)
        {
            Assert.False(interrupted, "A subsequent stage ran after cancellation.");
            stages.Add(current);
            if (current != stage) return;
            interrupted = true;
            if (deadline) clock.Advance(TimeSpan.FromSeconds(5)); else caller.Cancel();
            switch (result)
            {
                case "49": throw new LdapException(49, "SENSITIVE-LDAP");
                case "timeout": throw new TimeoutException("SENSITIVE-timeout");
                case "cancel": throw new OperationCanceledException("SENSITIVE-cancel");
                case "unexpected": throw new InvalidOperationException("SENSITIVE-internals");
            }
        }
        var factory = new FakeFactory(group, Visit);
        var credentials = new Credentials(Visit);
        DirectoryFailure? failure;
        if (group)
        {
            var service = new LdapService(Options.Create(Settings()), credentials, NullLogger<LdapService>.Instance, factory);
            var value = await service.GetUserGroupsAsync(User, "synthetic", caller.Token, operation);
            Assert.False(value.IsSuccess);
            Assert.Throws<InvalidOperationException>(() => value.Groups);
            failure = value.Failure;
        }
        else
        {
            var service = new LdapAuthenticationService(Options.Create(Settings()), new LdapAuthenticationClient(factory), credentials,
                NullLogger<LdapAuthenticationService>.Instance);
            var value = await service.AuthenticateAsync(User, "synthetic", caller.Token, operation);
            Assert.False(value.IsAuthenticated);
            failure = value.Failure;
        }
        Assert.True(interrupted);
        Assert.Equal(stage, failure!.Stage);
        Assert.Equal(deadline ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled, failure.Category);
        Assert.Equal(deadline ? DirectoryFailureReason.AuthenticationDeadlineExceeded : DirectoryFailureReason.CallerCancelled, failure.Reason);
        Assert.Null(failure.DiagnosticCode);
        Assert.DoesNotContain("SENSITIVE", failure.SafeMessage);
        Assert.Equal(stage, stages.Last());
        Assert.Equal(factory.Created ? 1 : 0, factory.Disposals);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BeforeStart_DoesNotCreateConnection(bool group, bool deadline)
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        if (deadline) clock.Advance(TimeSpan.FromSeconds(5), fire: false); else caller.Cancel();
        var factory = new FakeFactory(group, _ => Assert.Fail("Work started after interruption."));
        var credentials = new Credentials(_ => Assert.Fail("Credential loading started."));
        var failure = group
            ? (await new LdapService(Options.Create(Settings()), credentials, NullLogger<LdapService>.Instance, factory)
                .GetUserGroupsAsync(User, "synthetic", caller.Token, operation)).Failure
            : (await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Settings(), credentials, caller.Token, operation)).Failure;
        Assert.Equal(deadline ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled, failure!.Category);
        Assert.False(factory.Created);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightBind_IsAwaitedAndDisposedOnlyAfterItReturns(bool deadline)
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var factory = new FakeFactory(false, stage =>
        {
            if (stage != DirectoryFailureStage.UserBind) return;
            entered.SetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)), "Test release was not signalled.");
        });
        var task = new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Settings(), new Credentials(_ => { }), caller.Token, operation);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (deadline) clock.Advance(TimeSpan.FromSeconds(5)); else caller.Cancel();
            Assert.False(task.IsCompleted);
            Assert.Equal(0, factory.Disposals);
        }
        finally { release.Set(); }
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.IsAuthenticated);
        Assert.Equal(DirectoryFailureStage.UserBind, result.Failure!.Stage);
        Assert.Equal(deadline ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled, result.Failure.Category);
        Assert.Equal(1, factory.Disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdentityAndGroups_ShareOneBudget(bool empty)
    {
        var clock = new ManualClock();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), clock: clock);
        var identity = new FakeFactory(false, stage => { if (stage == DirectoryFailureStage.UserBind) clock.Advance(TimeSpan.FromSeconds(3)); });
        Assert.True((await new LdapAuthenticationClient(identity).AuthenticateAsync(User, "synthetic", Settings(), new Credentials(_ => { }), operation: operation)).IsAuthenticated);
        var groups = new FakeFactory(true, stage => { if (stage == DirectoryFailureStage.GroupSearch) clock.Advance(TimeSpan.FromSeconds(2)); }, empty);
        var result = await new LdapService(Options.Create(Settings()), new Credentials(_ => { }), NullLogger<LdapService>.Instance, groups)
            .GetUserGroupsAsync(User, "synthetic", operation: operation);
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthenticationFailureCategory.Timeout, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.GroupSearch, result.Failure.Stage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComfortableBudget_AllowsIdentityAndEmptyOrPopulatedGroups(bool empty)
    {
        var clock = new ManualClock();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(30), clock: clock);
        var credentials = new Credentials(_ => { });
        var identity = new FakeFactory(false, _ => clock.Advance(TimeSpan.FromMilliseconds(10)));
        Assert.True((await new LdapAuthenticationClient(identity).AuthenticateAsync(User, "synthetic", Settings(), credentials, operation: operation)).IsAuthenticated);
        var groups = new FakeFactory(true, _ => clock.Advance(TimeSpan.FromMilliseconds(10)), empty);
        var result = await new LdapService(Options.Create(Settings()), credentials, NullLogger<LdapService>.Instance, groups)
            .GetUserGroupsAsync(User, "synthetic", operation: operation);
        Assert.True(result.IsSuccess);
        Assert.Equal(empty ? 0 : 1, result.Groups.Count);
        operation.Complete();
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Null(operation.Failure);
        Assert.Equal(1, identity.Disposals);
        Assert.Equal(1, groups.Disposals);
    }

    [Theory]
    [InlineData("caller")]
    [InlineData("deadline")]
    [InlineData("tie")]
    public void OriginIsStable_AcrossRacesAndRepeatedSignals(string first)
    {
        for (var iteration = 0; iteration < 25; iteration++)
        {
            var clock = new ManualClock();
            using var caller = new CancellationTokenSource();
            using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
            operation.EnterStage(DirectoryFailureStage.ServiceBind);
            if (first == "caller") caller.Cancel();
            clock.Advance(TimeSpan.FromSeconds(5), fire: first != "tie");
            Parallel.Invoke(caller.Cancel, clock.Fire);
            var failure = operation.Failure;
            caller.Cancel(); clock.Fire();
            Assert.Same(failure, operation.Failure);
            Assert.Equal(first == "caller" ? AuthenticationFailureCategory.Cancelled : AuthenticationFailureCategory.Timeout, failure!.Category);
            Assert.Equal(DirectoryFailureStage.ServiceBind, failure.Stage);
            Assert.Throws<OperationCanceledException>(() => operation.EnterStage(DirectoryFailureStage.UserSearch));
            Assert.Throws<OperationCanceledException>(operation.Complete);
        }
    }

    [Fact]
    public void DelayedTimer_IsNotAnExtensionOfTheDeadline()
    {
        var clock = new ManualClock();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), clock: clock);
        operation.EnterStage(DirectoryFailureStage.UserBind);
        clock.Advance(TimeSpan.FromSeconds(6), fire: false);
        Assert.Throws<OperationCanceledException>(operation.Complete);
        Assert.Equal(AuthenticationFailureCategory.Timeout, operation.Failure!.Category);
        Assert.True(operation.Token.IsCancellationRequested);
    }

    [Fact]
    public void Disposal_UnregistersCallerAndTimerWithoutCancellingCaller()
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        operation.Dispose();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.False(caller.IsCancellationRequested);
        caller.Cancel();
        Assert.Null(operation.Failure);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(0.999, false)]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(60.001, false)]
    [InlineData(86400, false)]
    public void TimeoutConfiguration_IsBounded(double seconds, bool valid)
    {
        var settings = Settings();
        settings.AuthenticationTimeout = TimeSpan.FromSeconds(seconds);
        Assert.Equal(valid, LdapOptionsValidator.Validate(settings, true).Count == 0);
        if (!valid) Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationOperation(settings.AuthenticationTimeout));
        else using (new AuthenticationOperation(settings.AuthenticationTimeout)) { }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncCredentialLoading_ReceivesTheCombinedSignal(bool deadline)
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        var credentials = new WaitingCredentials();
        var factory = new FakeFactory(false, _ => Assert.Fail("LDAP started after credential cancellation."));
        var task = new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Settings(), credentials, caller.Token, operation);
        await credentials.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (deadline) clock.Advance(TimeSpan.FromSeconds(5)); else caller.Cancel();
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(deadline ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.CredentialLoading, result.Failure.Stage);
        Assert.True(credentials.Finished);
        Assert.False(factory.Created);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BindEntryPoint_RejectsLateSuccessAndPreservesStage(bool deadline)
    {
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        var factory = new FakeFactory(false, stage =>
        {
            // This seam labels its first bind ServiceBind; the direct BindAsync operation must label UserBind.
            if (stage != DirectoryFailureStage.ServiceBind) return;
            if (deadline) clock.Advance(TimeSpan.FromSeconds(5)); else caller.Cancel();
        });
        var result = await new LdapAuthenticationClient(factory).BindAsync(User, "synthetic", Settings(), caller.Token, operation);
        Assert.Equal(deadline ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.UserBind, result.Failure.Stage);
        Assert.Equal(1, factory.Disposals);
    }

    [Fact]
    public async Task AlreadyCancelledCaller_WithoutSuppliedContext_CannotStartAuthentication()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var factory = new FakeFactory(false, _ => Assert.Fail("LDAP must not run."));
        var result = await new LdapAuthenticationClient(factory).AuthenticateAsync(User, "synthetic", Settings(), new Credentials(_ => Assert.Fail("Credentials must not load.")), caller.Token);
        Assert.Equal(AuthenticationFailureCategory.Cancelled, result.Failure!.Category);
        Assert.False(factory.Created);
    }

    [Fact]
    public void CompletedContext_CannotBeReusedForUnboundedWork()
    {
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5));
        operation.Complete();
        Assert.Throws<InvalidOperationException>(() => operation.EnterStage(DirectoryFailureStage.ConnectionSetup));
        Assert.Throws<InvalidOperationException>(operation.ThrowIfCancellationRequested);
    }

    private sealed class WaitingCredentials : ILdapServiceAccountCredentialProvider
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Finished;
        public async Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            Entered.SetResult();
            try { await Task.Delay(Timeout.Infinite, cancellationToken); return "unreachable"; }
            finally { Finished = true; }
        }
    }

    private static LdapOptions Settings() => new()
    {
        Domain = "lab.local", Host = "directory.example.test", BaseDn = "DC=lab,DC=local", UserSearchBaseDn = "DC=lab,DC=local",
        ServiceAccountUsername = "service@lab.local"
    };

    private sealed class Credentials(Action<DirectoryFailureStage> visit) : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            Assert.True(cancellationToken.CanBeCanceled);
            visit(DirectoryFailureStage.CredentialLoading);
            return Task.FromResult("synthetic-service");
        }
    }

    private sealed class FakeFactory(bool group, Action<DirectoryFailureStage> visit, bool empty = false) : ILdapConnectionFactory, ILdapConnection
    {
        private int _binds;
        public bool Created { get; private set; }
        public int Disposals;
        public ILdapConnection Create(LdapOptions options) { Created = true; return this; }
        public void ConfigureSession(bool useLdaps) => visit(DirectoryFailureStage.ConnectionSetup);
        public void Bind(NetworkCredential credential) => visit(++_binds == 1 ? DirectoryFailureStage.ServiceBind : DirectoryFailureStage.UserBind);
        public LdapSearchResult Search(SearchRequest request)
        {
            visit(group ? DirectoryFailureStage.GroupSearch : DirectoryFailureStage.UserSearch);
            const string dn = "CN=Reader,DC=lab,DC=local";
            var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["distinguishedName"] = [dn], ["userPrincipalName"] = [User], ["userAccountControl"] = ["512"]
            };
            if (group) attributes["memberOf"] = empty ? [] : ["CN=GG-APP-USER,DC=lab,DC=local"];
            return new(ResultCode.Success, [new(dn, new ObservedAttributes(attributes, () => visit(DirectoryFailureStage.ResponseValidation)))]);
        }
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }

    private sealed class ObservedAttributes(IReadOnlyDictionary<string, IReadOnlyList<string>> inner, Action visit) : IReadOnlyDictionary<string, IReadOnlyList<string>>
    {
        private bool _visited;
        private void Observe() { if (!_visited) { _visited = true; visit(); } }
        public IEnumerable<string> Keys { get { Observe(); return inner.Keys; } }
        public IEnumerable<IReadOnlyList<string>> Values => inner.Values;
        public int Count => inner.Count;
        public IReadOnlyList<string> this[string key] => inner[key];
        public bool ContainsKey(string key) => inner.ContainsKey(key);
        public bool TryGetValue(string key, out IReadOnlyList<string> value) { Observe(); return inner.TryGetValue(key, out value!); }
        public IEnumerator<KeyValuePair<string, IReadOnlyList<string>>> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;
        private Timer? _timer;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => _timer = new Timer(callback, state, GetTimestamp() + dueTime.Ticks);
        public void Advance(TimeSpan time, bool fire = true) { Interlocked.Add(ref _ticks, time.Ticks); if (fire) Fire(); }
        public void Fire() { if (_timer is { Disposed: false } timer && GetTimestamp() >= timer.Due) timer.Callback(timer.State); }
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
