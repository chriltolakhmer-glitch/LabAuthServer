using LabAuthServer.Application.Auditing;

namespace LabAuthServer.Application.Interfaces;

public interface IAuditEventService
{
    Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
