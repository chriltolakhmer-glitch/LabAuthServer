using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class LdapPendingWaiterTests
{
    [Fact]
    public void Default_IsSixteen() => Assert.Equal(16, new LdapOptions().MaxPendingLdapWaiters);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(129)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void InvalidConfiguration_RejectsBeforeAllocation(int cap)
    {
        var options = Settings(1, cap);
        Assert.Contains(LdapOptionsValidator.Validate(options, true), value => value.Contains("pending LDAP"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LdapConcurrencyLimiter(Options.Create(options)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(128)]
    public async Task ConfiguredCap_RejectsExcessWithoutWaiting_AndRecovers(int cap)
    {
        Assert.Empty(LdapOptionsValidator.Validate(Settings(1, cap), true));
        using var gate = Gate(1, cap);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        Assert.True(held.IsSuccess);
        Assert.Equal(0, gate.PendingWaiterCount);
        using var cancellation = new CancellationTokenSource();
        var operations = Enumerable.Range(0, cap).Select(_ => Operation(cancellation.Token)).ToArray();
        try
        {
            var waiting = operations.Select(gate.AcquireAsync).ToArray();
            Assert.All(waiting, task => Assert.False(task.IsCompleted));
            Assert.Equal(cap, gate.PendingWaiterCount);
            using var excess = Operation();
            var rejected = gate.AcquireAsync(excess);
            Assert.True(rejected.IsCompletedSuccessfully); // Never becomes a semaphore waiter.
            using var rejection = await rejected;
            AssertCapacity(rejection);
            Assert.Equal(cap, gate.PendingWaiterCount);
            cancellation.Cancel(); cancellation.Cancel();
            foreach (var task in waiting) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(0, gate.PendingWaiterCount);
            held.Dispose(); held.Dispose();
            using var next = Operation();
            using var acquired = await gate.AcquireAsync(next);
            Assert.True(acquired.IsSuccess);
            Assert.Equal(0, gate.PendingWaiterCount);
            AssertCapacity(rejection); // A rejected request cannot rejoin later.
        }
        finally { cancellation.Cancel(); foreach (var operation in operations) operation.Dispose(); }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(32)]
    public async Task ActivePermits_DoNotConsumePendingCapacity(int active)
    {
        using var gate = Gate(active, 1);
        var operations = Enumerable.Range(0, active).Select(_ => Operation()).ToArray();
        var leases = new List<LdapAdmissionResult>();
        using var cancel = new CancellationTokenSource();
        try
        {
            foreach (var op in operations) { var lease = await gate.AcquireAsync(op); Assert.True(lease.IsSuccess); leases.Add(lease); }
            Assert.Equal(0, gate.PendingWaiterCount);
            using var pending = Operation(cancel.Token);
            var wait = gate.AcquireAsync(pending);
            Assert.Equal(1, gate.PendingWaiterCount);
            using var excess = Operation();
            using var rejected = await gate.AcquireAsync(excess);
            AssertCapacity(rejected);
            leases[0].Dispose();
            using var admitted = await wait.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(admitted.IsSuccess);
            Assert.Equal(0, gate.PendingWaiterCount);
        }
        finally { cancel.Cancel(); foreach (var lease in leases) lease.Dispose(); foreach (var op in operations) op.Dispose(); }
    }

    [Theory]
    [InlineData("caller")]
    [InlineData("deadline")]
    [InlineData("caller-first")]
    [InlineData("deadline-first")]
    [InlineData("equality")]
    public async Task RepeatedInterruption_ReleasesPendingCapacity_AndPreservesOrigin(string origin)
    {
        using var gate = Gate(1, 1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        for (var cycle = 0; cycle < 20; cycle++)
        {
            var clock = new ManualClock();
            using var caller = new CancellationTokenSource();
            using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
            var waiting = gate.AcquireAsync(operation);
            Assert.False(waiting.IsCompleted);
            Assert.Equal(1, gate.PendingWaiterCount);
            Interrupt(origin, caller, clock);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(Expected(origin), operation.Failure!.Category);
            Assert.Equal(DirectoryFailureStage.ConcurrencyWait, operation.Failure.Stage);
            var failure = operation.Failure;
            caller.Cancel(); clock.Advance();
            Assert.Same(failure, operation.Failure);
            Assert.Equal(0, gate.PendingWaiterCount);
        }
        using var replacement = Operation();
        var pending = gate.AcquireAsync(replacement);
        Assert.False(pending.IsCompleted);
        held.Dispose();
        using var result = await pending.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, gate.PendingWaiterCount);
    }

    [Theory]
    [InlineData("caller", false)]
    [InlineData("deadline", false)]
    [InlineData("equality", false)]
    [InlineData("caller", true)]
    [InlineData("deadline", true)]
    [InlineData("equality", true)]
    public async Task InterruptedBeforeAdmission_NeverBecomesCapacityFailure(string origin, bool full)
    {
        using var gate = Gate(1, 1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        using var cancelWaiter = new CancellationTokenSource();
        using var existing = Operation(cancelWaiter.Token);
        var pending = full ? gate.AcquireAsync(existing) : null;
        try
        {
            var clock = new ManualClock();
            using var caller = new CancellationTokenSource();
            using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), caller.Token, clock);
            Interrupt(origin, caller, clock);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.AcquireAsync(operation));
            Assert.Equal(Expected(origin), operation.Failure!.Category);
            Assert.Equal(full ? 1 : 0, gate.PendingWaiterCount);
        }
        finally
        {
            cancelWaiter.Cancel();
            if (pending is not null) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        Assert.Equal(0, gate.PendingWaiterCount);
    }

    [Fact]
    public async Task DelayedTimerAtHandoff_ReturnsPermitAndPendingCapacity()
    {
        using var gate = Gate(1, 1);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        var clock = new ManualClock();
        using var operation = new AuthenticationOperation(TimeSpan.FromSeconds(5), clock: clock);
        var waiting = gate.AcquireAsync(operation);
        clock.Advance(fire: false);
        Assert.Equal(1, gate.PendingWaiterCount);
        held.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(AuthenticationFailureCategory.Timeout, operation.Failure!.Category);
        Assert.Equal(0, gate.PendingWaiterCount);
        using var replacement = Operation();
        using var admitted = await gate.AcquireAsync(replacement);
        Assert.True(admitted.IsSuccess);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(128)]
    public async Task SimultaneousAdmission_NeverExceedsCap(int cap)
    {
        using var gate = Gate(1, cap);
        using var holder = Operation();
        using var held = await gate.AcquireAsync(holder);
        using var caller = new CancellationTokenSource();
        var operations = Enumerable.Range(0, cap + 32).Select(_ => Operation(caller.Token)).ToArray();
        var work = new Task<LdapAdmissionResult>[operations.Length];
        try
        {
            Parallel.For(0, operations.Length, index =>
            {
                work[index] = gate.AcquireAsync(operations[index]);
                Assert.InRange(gate.PendingWaiterCount, 0, cap);
            });
            Assert.Equal(cap, gate.PendingWaiterCount);
            var pending = work.Where(task => !task.IsCompleted).ToArray();
            Assert.Equal(cap, pending.Length);
            var rejected = work.Where(task => task.IsCompleted).ToArray();
            Assert.Equal(32, rejected.Length);
            foreach (var task in rejected) { using var result = await task; AssertCapacity(result); }
            caller.Cancel();
            foreach (var task in pending) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(0, gate.PendingWaiterCount);
            held.Dispose();
            using var next = Operation();
            using var lease = await gate.AcquireAsync(next);
            Assert.True(lease.IsSuccess);
        }
        finally { caller.Cancel(); foreach (var operation in operations) operation.Dispose(); }
    }

    [Fact]
    public void AdmissionResult_HasOneState_AndDisposesOwnedPermitExactlyOnce()
    {
        var lease = new CountingLease();
        using var result = LdapAdmissionResult.Succeeded(lease);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Parallel.For(0, 20, _ => result.Dispose());
        Assert.Equal(1, lease.Disposals);
        Assert.Throws<ArgumentNullException>(() => LdapAdmissionResult.Succeeded(null!));
        Assert.Throws<ArgumentNullException>(() => LdapAdmissionResult.Failed(null!));
    }

    private static void AssertCapacity(LdapAdmissionResult result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthenticationFailureCategory.ResourceExhausted, result.Failure!.Category);
        Assert.Equal(DirectoryFailureStage.ConcurrencyWait, result.Failure.Stage);
        Assert.Equal(DirectoryFailureReason.PendingWaiterCapacityExceeded, result.Failure.Reason);
        Assert.Equal("Authentication service unavailable.", result.Failure.SafeMessage);
        Assert.Null(result.Failure.DiagnosticCode);
        Assert.Equal(DirectoryDiagnosticSource.None, result.Failure.DiagnosticSource);
    }

    private static AuthenticationFailureCategory Expected(string origin)
        => origin is "caller" or "caller-first" ? AuthenticationFailureCategory.Cancelled : AuthenticationFailureCategory.Timeout;
    private static void Interrupt(string origin, CancellationTokenSource caller, ManualClock clock)
    {
        if (origin is "caller" or "caller-first") caller.Cancel();
        if (origin is "deadline" or "deadline-first" or "caller-first") clock.Advance();
        if (origin == "deadline-first") caller.Cancel();
        if (origin == "equality") { clock.Advance(fire: false); Parallel.Invoke(caller.Cancel, clock.Fire); }
    }
    private static LdapOptions Settings(int active, int cap) => new()
    {
        Domain = "lab.local", Host = "directory.example.test", BaseDn = "DC=lab,DC=local",
        UserSearchBaseDn = "DC=lab,DC=local", ServiceAccountUsername = "service@lab.local",
        MaxConcurrentLdapOperations = active, MaxPendingLdapWaiters = cap
    };
    private static LdapConcurrencyLimiter Gate(int active, int cap) => new(Options.Create(Settings(active, cap)));
    private static AuthenticationOperation Operation(CancellationToken caller = default) => new(TimeSpan.FromSeconds(60), caller);
    private sealed class CountingLease : IDisposable
    {
        public int Disposals;
        public void Dispose() => Interlocked.Increment(ref Disposals);
    }
    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;
        private Timer? _timer;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => _timer = new Timer(callback, state);
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
