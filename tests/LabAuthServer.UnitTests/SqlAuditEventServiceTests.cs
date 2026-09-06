using LabAuthServer.Application.Auditing;
using LabAuthServer.Infrastructure.Auditing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class SqlAuditEventServiceTests
{
    private const string ConnectionString =
        "Server=localhost;Database=LabAuthServer;Integrated Security=True;Encrypt=False;Application Name=LabAuthServer.Phase12.Tests";

    [Fact]
    public async Task WriteAsync_PersistsApprovedEventAndReturnsAuditEventId()
    {
        var correlationId = Guid.NewGuid();
        var service = CreateService();
        var auditEvent = CreateEvent(correlationId);

        var auditEventId = await service.WriteAsync(auditEvent);

        Assert.True(auditEventId > 0);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT EventTypeId, CorrelationId, StatusCode, Success, DetailsJson FROM Audit.AuditEvents WHERE AuditEventId = @AuditEventId",
            connection);
        command.Parameters.AddWithValue("@AuditEventId", auditEventId);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(correlationId, reader.GetGuid(1));
        Assert.Equal((short)401, reader.GetInt16(2));
        Assert.False(reader.GetBoolean(3));
        Assert.Equal(auditEvent.DetailsJson, reader.GetString(4));
    }

    [Fact]
    public async Task WriteAsync_RejectsInvalidEventBeforeDatabaseAccess()
    {
        var service = CreateService();
        var auditEvent = CreateEvent(Guid.NewGuid()) with
        {
            EventTypeCode = "NOT_APPROVED",
            DetailsJson = "{\"password\":\"placeholder\"}"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.WriteAsync(auditEvent));

        Assert.Contains("approved", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("placeholder", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteAsync_WhenDatabaseUnavailableReturnsNull()
    {
        var options = Options.Create(new AuditOptions
        {
            ConnectionString = "Server=invalid-phase12-host;Database=LabAuthServer;Integrated Security=True;Connect Timeout=1",
            CommandTimeoutSeconds = 1
        });
        var service = new SqlAuditEventService(options, NullLogger<SqlAuditEventService>.Instance);

        var result = await service.WriteAsync(CreateEvent(Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task WriteAsync_ConcurrentEventsPersistWithIsolatedCorrelationIds()
    {
        var service = CreateService();
        var events = Enumerable.Range(0, 4)
            .Select(_ => CreateEvent(Guid.NewGuid()))
            .ToArray();

        var ids = await Task.WhenAll(events.Select(auditEvent => service.WriteAsync(auditEvent)));

        Assert.All(ids, id => Assert.True(id > 0));
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    private static SqlAuditEventService CreateService() => new(
        Options.Create(new AuditOptions
        {
            ConnectionString = ConnectionString,
            CommandTimeoutSeconds = 5
        }),
        NullLogger<SqlAuditEventService>.Instance);

    private static AuditEvent CreateEvent(Guid correlationId) => new()
    {
        EventTypeCode = AuditEventTypes.LoginFailure,
        EventTimeUtc = DateTime.UtcNow,
        CorrelationId = correlationId,
        RequestId = "phase12-test-request",
        Username = "phase12-test-user@lab.local",
        Endpoint = "/api/v1/auth/login",
        HttpMethod = "POST",
        StatusCode = 401,
        Success = false,
        DetailsJson = "{\"failureCategory\":\"InvalidCredentials\"}"
    };
}