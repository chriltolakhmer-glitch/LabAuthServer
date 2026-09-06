using System.Data;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Auditing;

public sealed class SqlAuditEventService : IAuditEventService
{
    private readonly IOptions<AuditOptions> _options;
    private readonly ILogger<SqlAuditEventService> _logger;

    public SqlAuditEventService(IOptions<AuditOptions> options, ILogger<SqlAuditEventService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var failures = AuditEventValidator.Validate(auditEvent);
        if (failures.Count > 0)
            throw new ArgumentException(string.Join(" ", failures), nameof(auditEvent));

        try
        {
            await using var connection = new SqlConnection(_options.Value.ConnectionString);
            await using var command = new SqlCommand("Audit.usp_WriteAuditEvent", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _options.Value.CommandTimeoutSeconds
            };

            Add(command, "@EventTypeCode", SqlDbType.VarChar, 64, auditEvent.EventTypeCode);
            Add(command, "@EventTimeUtc", SqlDbType.DateTime2, value: auditEvent.EventTimeUtc);
            Add(command, "@CorrelationId", SqlDbType.UniqueIdentifier, value: auditEvent.CorrelationId);
            Add(command, "@RequestId", SqlDbType.VarChar, 128, auditEvent.RequestId);
            Add(command, "@Username", SqlDbType.NVarChar, 256, auditEvent.Username);
            Add(command, "@Subject", SqlDbType.NVarChar, 256, auditEvent.Subject);
            Add(command, "@Role", SqlDbType.NVarChar, 64, auditEvent.Role);
            Add(command, "@Endpoint", SqlDbType.NVarChar, 256, auditEvent.Endpoint);
            Add(command, "@HttpMethod", SqlDbType.VarChar, 16, auditEvent.HttpMethod);
            Add(command, "@StatusCode", SqlDbType.SmallInt, value: auditEvent.StatusCode);
            Add(command, "@Success", SqlDbType.Bit, value: auditEvent.Success);
            Add(command, "@ClientIp", SqlDbType.VarChar, 45, auditEvent.ClientIp);
            Add(command, "@ServerName", SqlDbType.NVarChar, 128, auditEvent.ServerName);
            Add(command, "@ApplicationVersion", SqlDbType.VarChar, 64, auditEvent.ApplicationVersion);
            Add(command, "@DetailsJson", SqlDbType.NVarChar, -1, auditEvent.DetailsJson);

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Audit procedure returned no result.");

            return reader.GetInt64(reader.GetOrdinal("AuditEventId"));
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            _logger.LogError("Audit persistence failed for event type {EventTypeCode} and correlation {CorrelationId}.", auditEvent.EventTypeCode, auditEvent.CorrelationId);
            return null;
        }
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, int? size = null, object? value = null)
    {
        var parameter = command.Parameters.Add(name, type);
        if (size.HasValue) parameter.Size = size.Value;
        parameter.Value = value ?? DBNull.Value;
    }
}
