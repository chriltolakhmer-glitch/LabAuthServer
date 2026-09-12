using System.Collections.Concurrent;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;

namespace LabAuthServer.IntegrationTests;

public sealed class RecordingAuditEventService : IAuditEventService
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    private readonly ConcurrentQueue<string> _validationFailures = new();
    private long _nextId;

    public IReadOnlyList<AuditEvent> Events => _events.ToArray();
    // Retained separately because production deliberately catches audit failures.
    public IReadOnlyList<string> ValidationFailures => _validationFailures.ToArray();

    public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var failures = AuditEventValidator.Validate(auditEvent);
        foreach (var failure in failures) _validationFailures.Enqueue(failure);
        if (failures.Count != 0)
            throw new ArgumentException(string.Join(" ", failures), nameof(auditEvent));

        cancellationToken.ThrowIfCancellationRequested();
        _events.Enqueue(auditEvent);
        return Task.FromResult<long?>(Interlocked.Increment(ref _nextId));
    }
}
