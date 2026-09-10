using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;

namespace LabAuthServer.Application.Services;

/// <summary>One cooperative decision budget. It never interrupts or detaches native work.</summary>
public sealed class AuthenticationOperation : IDisposable
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    public static bool IsValidTimeout(TimeSpan timeout) => timeout >= TimeSpan.FromSeconds(1) && timeout <= TimeSpan.FromSeconds(60);

    private readonly object _gate = new();
    private readonly TimeProvider _clock;
    private readonly long _started;
    private readonly TimeSpan _timeout;
    private readonly CancellationToken _caller;
    private readonly CancellationTokenSource _signal = new();
    private readonly CancellationTokenRegistration _registration;
    private readonly ITimer _timer;
    private DirectoryFailureStage _stage = DirectoryFailureStage.CredentialLoading;
    private DirectoryFailure? _failure;
    private bool _completed;
    private bool _disposed;

    public AuthenticationOperation(TimeSpan timeout, CancellationToken caller = default, TimeProvider? clock = null)
    {
        if (!IsValidTimeout(timeout)) throw new ArgumentOutOfRangeException(nameof(timeout));
        _clock = clock ?? TimeProvider.System;
        _timeout = timeout;
        _caller = caller;
        _started = _clock.GetTimestamp();
        Token = _signal.Token;
        // Registration invokes synchronously for an already-cancelled caller.
        _registration = caller.Register(() => Observe());
        _timer = _clock.CreateTimer(_ => Observe(), null, timeout, Timeout.InfiniteTimeSpan);
    }

    public CancellationToken Token { get; }
    public DirectoryFailureStage Stage { get { lock (_gate) return _stage; } }
    public DirectoryFailure? Failure { get { Observe(); lock (_gate) return _failure; } }

    public void EnterStage(DirectoryFailureStage stage)
    {
        if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        Observe(nextStage: stage);
        lock (_gate) ThrowIfFailed();
    }

    public void ThrowIfCancellationRequested()
    {
        Observe();
        lock (_gate) ThrowIfFailed();
    }

    /// <summary>Linearizes successful login before best-effort audit/HTTP delivery.</summary>
    public void Complete()
    {
        Observe(complete: true);
        lock (_gate) ThrowIfFailed(allowCompleted: true);
    }

    private void ThrowIfFailed(bool allowCompleted = false)
    {
        if (_failure is not null) throw new OperationCanceledException(Token);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed && !allowCompleted) throw new InvalidOperationException("The authentication operation is complete.");
    }

    private void Observe(DirectoryFailureStage? nextStage = null, bool complete = false)
    {
        bool signal;
        lock (_gate)
        {
            if (_disposed || _completed || _failure is not null) return;
            // The monotonic deadline wins at equality, even if its timer callback is delayed.
            var expired = _clock.GetElapsedTime(_started) >= _timeout;
            signal = expired || _caller.IsCancellationRequested;
            if (signal)
                _failure = new(expired ? AuthenticationFailureCategory.Timeout : AuthenticationFailureCategory.Cancelled,
                    _stage, expired ? DirectoryFailureReason.AuthenticationDeadlineExceeded : DirectoryFailureReason.CallerCancelled);
            else
            {
                if (nextStage.HasValue) _stage = nextStage.Value;
                _completed = complete;
            }
        }
        if (signal)
        {
            // Consumer callbacks cannot change the recorded outcome or escape a timer callback.
            try { _signal.Cancel(); }
            catch (AggregateException) { }
            catch (ObjectDisposedException) { }
        }
    }

    public void Dispose()
    {
        lock (_gate) _disposed = true;
        _timer.Dispose();
        _registration.Dispose();
        _signal.Dispose();
    }
}
