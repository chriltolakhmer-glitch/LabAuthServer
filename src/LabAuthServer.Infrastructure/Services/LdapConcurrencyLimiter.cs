using LabAuthServer.Application.Enums;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.ActiveDirectory;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Services;

/// <summary>Singleton gate; waiting uses the existing operation's signal and decision deadline.</summary>
public sealed class LdapConcurrencyLimiter : ILdapConcurrencyLimiter, IDisposable
{
    private readonly SemaphoreSlim _slots;
    private readonly object _admissionLock = new();
    private readonly HashSet<Task> _pending = [];
    private readonly int _maximumPending;

    public LdapConcurrencyLimiter(IOptions<LdapOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var limit = options.Value.MaxConcurrentLdapOperations;
        if (limit is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(options));
        _maximumPending = options.Value.MaxPendingLdapWaiters;
        if (_maximumPending is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(options));
        _slots = new SemaphoreSlim(limit, limit);
    }

    public async Task<LdapAdmissionResult> AcquireAsync(AuthenticationOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        operation.EnterStage(DirectoryFailureStage.ConcurrencyWait);
        Task waiting;
        lock (_admissionLock)
        {
            operation.ThrowIfCancellationRequested();
            RemoveCompletedWaits();
            // All permit acquisitions pass this lock. Releases can only add availability;
            // a positive CurrentCount therefore permits immediate acquisition without a waiter.
            if (_slots.CurrentCount > 0)
                waiting = _slots.WaitAsync(operation.Token);
            else
            {
                if (_pending.Count >= _maximumPending)
                {
                    // Preserve caller/deadline precedence before selecting the capacity outcome.
                    operation.ThrowIfCancellationRequested();
                    return LdapAdmissionResult.Failed(new(AuthenticationFailureCategory.ResourceExhausted,
                        DirectoryFailureStage.ConcurrencyWait, DirectoryFailureReason.PendingWaiterCapacityExceeded));
                }

                // Admission and registration are atomic. No over-cap call reaches WaitAsync.
                waiting = _slots.WaitAsync(operation.Token);
                if (!waiting.IsCompleted) _pending.Add(waiting);
            }
        }

        try { await waiting.ConfigureAwait(false); }
        finally
        {
            lock (_admissionLock) _pending.Remove(waiting);
        }
        try
        {
            // A permit may become available concurrently with cancellation or a delayed timer.
            operation.ThrowIfCancellationRequested();
            return LdapAdmissionResult.Succeeded(new Lease(_slots));
        }
        catch
        {
            _slots.Release();
            throw;
        }
    }

    // The set is bounded by the cap. A completed wait no longer reserves pending capacity,
    // even if its request continuation has not executed the finally above yet.
    private void RemoveCompletedWaits() => _pending.RemoveWhere(static waiting => waiting.IsCompleted);

    internal int PendingWaiterCount
    {
        get { lock (_admissionLock) { RemoveCompletedWaits(); return _pending.Count; } }
    }

    // The host owns this singleton and disposes it after request work is drained.
    public void Dispose() => _slots.Dispose();

    private sealed class Lease(SemaphoreSlim slots) : IDisposable
    {
        private SemaphoreSlim? _slots = slots;
        public void Dispose() => Interlocked.Exchange(ref _slots, null)?.Release();
    }
}
