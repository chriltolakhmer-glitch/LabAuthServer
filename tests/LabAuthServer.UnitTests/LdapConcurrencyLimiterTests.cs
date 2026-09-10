using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class LdapConcurrencyLimiterTests
{
    [Fact]
    public void DefaultLimit_IsFour() => Assert.Equal(4, new LdapOptions().MaxConcurrentLdapOperations);

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(33)]
    [InlineData(int.MaxValue)]
    public void InvalidLimit_IsRejectedByValidatorAndConstructor(int limit)
    {
        var options = Settings(limit);
        Assert.Contains(LdapOptionsValidator.Validate(options, true), value => value.Contains("concurrent LDAP"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LdapConcurrencyLimiter(Options.Create(options)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(32)]
    public async Task UpToLimitEnter_ExcessWaits_ReleaseAllowsNext(int limit)
    {
        Assert.Empty(LdapOptionsValidator.Validate(Settings(limit), true));
        using var gate = Gate(limit);
        var leases = new List<IDisposable>();
        var operations = new List<AuthenticationOperation>();
        try
        {
            for (var index = 0; index < limit; index++)
            {
                var operation = Operation(); operations.Add(operation);
                var pending = gate.AcquireAsync(operation);
                Assert.True(pending.IsCompletedSuccessfully);
                leases.Add(await pending);
            }
            using var waiter = Operation();
            var waiting = gate.AcquireAsync(waiter);
            Assert.False(waiting.IsCompleted);
            Assert.Equal(DirectoryFailureStage.ConcurrencyWait, waiter.Stage);
            leases[0].Dispose();
            using var next = await waiting.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Null(waiter.Failure);
        }
        finally { foreach (var lease in leases) lease.Dispose(); foreach (var operation in operations) operation.Dispose(); }
    }

    [Theory]
    [InlineData("caller")]
    [InlineData("deadline")]
    [InlineData("equality")]
    [InlineData("caller-first")]
    [InlineData("deadline-first")]
    public async Task InterruptedWaiter_NeverConsumesReleasedSlot_AndOriginRemainsStable(string origin)
    {
        using var gate = Gate(1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        var clock = new ManualClock();
        using var caller = new CancellationTokenSource();
        using var waiter = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        var waiting = gate.AcquireAsync(waiter);
        Assert.False(waiting.IsCompleted);
        if (origin is "caller" or "caller-first") caller.Cancel();
        if (origin is "deadline" or "deadline-first" or "caller-first") clock.Advance();
        if (origin == "equality") { clock.Advance(fire: false); Parallel.Invoke(caller.Cancel, clock.Fire); }
        if (origin == "deadline-first") caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(10)));
        var failure = waiter.Failure;
        Assert.Equal(origin is "caller" or "caller-first" ? AuthenticationFailureCategory.Cancelled : AuthenticationFailureCategory.Timeout, failure!.Category);
        Assert.Equal(DirectoryFailureStage.ConcurrencyWait, failure.Stage);
        Assert.Null(failure.DiagnosticCode);
        Assert.Null(holder.Failure);
        caller.Cancel(); clock.Advance();
        Assert.Same(failure, waiter.Failure);
        held.Dispose();
        using var replacement = Operation();
        var admission = gate.AcquireAsync(replacement);
        Assert.True(admission.IsCompletedSuccessfully);
        using var lease = await admission;
        Assert.True(waiting.IsCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AlreadyInterrupted_DoesNotAcquireEvenWithCapacity(bool deadline)
    {
        using var gate = Gate(1);
        using var caller = new CancellationTokenSource();
        var clock = new ManualClock();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
        if (deadline) clock.Advance(fire: false); else caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.AcquireAsync(operation));
        using var next = Operation();
        var acquire = gate.AcquireAsync(next);
        Assert.True(acquire.IsCompletedSuccessfully);
        using var lease = await acquire;
    }

    [Fact]
    public async Task DelayedDeadlineCallback_RejectsPermitAtHandoff_WithoutLeakingIt()
    {
        using var gate = Gate(1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        var clock = new ManualClock();
        using var waiter = new AuthenticationOperation(TimeSpan.FromSeconds(5), clock: clock);
        var waiting = gate.AcquireAsync(waiter);
        clock.Advance(fire: false);
        Assert.False(waiting.IsCompleted);
        held.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(AuthenticationFailureCategory.Timeout, waiter.Failure!.Category);
        using var next = Operation();
        var admission = gate.AcquireAsync(next);
        Assert.True(admission.IsCompletedSuccessfully);
        using var lease = await admission;
    }

    [Fact]
    public async Task LeaseDisposal_IsIdempotentEvenAcrossThreads()
    {
        using var gate = Gate(1);
        using var owner = Operation();
        var lease = await gate.AcquireAsync(owner);
        Parallel.For(0, 50, _ => lease.Dispose());
        using var next = Operation();
        using var held = await gate.AcquireAsync(next);
        using var cancelled = new CancellationTokenSource();
        using var excess = new AuthenticationOperation(TimeSpan.FromSeconds(30), cancelled.Token);
        var waiting = gate.AcquireAsync(excess);
        Assert.False(waiting.IsCompleted); // Double release would have created excess capacity.
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task ControlledContention_NeverExceedsLimit_AndAllCapacityIsReusable(int limit)
    {
        // This existing test admits all 24 contenders to verify active-permit ownership.
        // Give it explicit pending capacity; cap rejection is tested separately.
        var settings = Settings(limit);
        settings.MaxPendingLdapWaiters = 24;
        using var gate = new LdapConcurrencyLimiter(Options.Create(settings));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var full = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var total = 0;
        async Task Run()
        {
            using var operation = Operation();
            using var lease = await gate.AcquireAsync(operation);
            Assert.True(lease.IsSuccess);
            Assert.InRange(Interlocked.Increment(ref active), 1, limit);
            if (Interlocked.Increment(ref total) == limit) full.TrySetResult();
            try { await release.Task; }
            finally { Interlocked.Decrement(ref active); }
        }
        var work = Enumerable.Range(0, 24).Select(_ => Run()).ToArray();
        try { await full.Task.WaitAsync(TimeSpan.FromSeconds(10)); Assert.Equal(limit, Volatile.Read(ref active)); Assert.Equal(limit, Volatile.Read(ref total)); }
        finally { release.TrySetResult(); }
        await Task.WhenAll(work).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(24, total);
        Assert.Equal(0, active);
        using var final = Operation();
        var admission = gate.AcquireAsync(final);
        Assert.True(admission.IsCompletedSuccessfully);
        using var last = await admission;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedInterruptedWaiters_DoNotLeakOrIncreaseCapacity(bool deadline)
    {
        using var gate = Gate(1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        for (var iteration = 0; iteration < 40; iteration++)
        {
            var clock = new ManualClock();
            using var caller = new CancellationTokenSource();
            using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
            var waiting = gate.AcquireAsync(operation);
            Assert.False(waiting.IsCompleted);
            if (deadline) clock.Advance(); else caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        }
        held.Dispose();
        using var next = Operation();
        var admission = gate.AcquireAsync(next);
        Assert.True(admission.IsCompletedSuccessfully);
        using var lease = await admission;
    }

    private static LdapOptions Settings(int limit) => new()
    {
        Host = "directory.example.test", Domain = "lab.local", BaseDn = "DC=lab,DC=local",
        UserSearchBaseDn = "DC=lab,DC=local", ServiceAccountUsername = "service@lab.local", MaxConcurrentLdapOperations = limit
    };
    private static LdapConcurrencyLimiter Gate(int limit) => new(Options.Create(Settings(limit)));
    private static AuthenticationOperation Operation() => new(TimeSpan.FromSeconds(60));

    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;
        private Timer? _timer;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => _timer = new Timer(callback, state);
        public void Advance(bool fire = true) { Interlocked.Add(ref _ticks, TimeSpan.FromSeconds(5).Ticks); if (fire) Fire(); }
        public void Fire() { if (_timer is { Disposed: false } timer) timer.Callback(timer.State); }
        private sealed class Timer(TimerCallback callback, object? state) : ITimer
        {
            public TimerCallback Callback { get; } = callback;
            public object? State { get; } = state;
            public bool Disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() => Disposed = true;
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
