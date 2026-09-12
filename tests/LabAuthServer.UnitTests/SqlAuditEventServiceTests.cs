using LabAuthServer.Application.Auditing;
using LabAuthServer.Infrastructure.Auditing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class SqlAuditEventServiceTests
{
    [Fact]
    public void DefaultConfiguration_EnablesSqlEncryption()
    {
        var configurationRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (configurationRoot is not null &&
               !File.Exists(Path.Combine(configurationRoot.FullName, "LabAuthServer.slnx")))
        {
            configurationRoot = configurationRoot.Parent;
        }

        Assert.NotNull(configurationRoot);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(configurationRoot!.FullName, "src", "LabAuthServer.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var connectionString = configuration["Audit:ConnectionString"];

        Assert.False(string.IsNullOrWhiteSpace(connectionString));
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.True(builder.Encrypt);
        Assert.DoesNotContain("Encrypt=False", connectionString, StringComparison.OrdinalIgnoreCase);
    }

    private static string ConnectionString =>
        SqlTestConfiguration.RequireConnectionString(Environment.GetEnvironmentVariable);

    [SqlInfrastructureFact]
    [Trait("Category", "SqlInfrastructure")]
    public async Task WriteAsync_PersistsApprovedEventAndReturnsAuditEventId()
    {
        var correlationId = Guid.NewGuid();
        var service = CreateService(ConnectionString);
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
        // Invalid events must be rejected before even reading connection configuration.
        var service = new SqlAuditEventService(new ForbiddenAuditOptions(), NullLogger<SqlAuditEventService>.Instance);
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
    public async Task WriteAsync_WhenConnectionIsUnconfiguredReturnsNullWithoutNetworkAccess()
    {
        var options = Options.Create(new AuditOptions
        {
            // SqlClient rejects an uninitialized connection locally; no DNS/socket/SQL target.
            ConnectionString = string.Empty,
            CommandTimeoutSeconds = 1
        });
        var service = new SqlAuditEventService(options, NullLogger<SqlAuditEventService>.Instance);

        var result = await service.WriteAsync(CreateEvent(Guid.NewGuid()));

        Assert.Null(result);
    }

    [SqlInfrastructureFact]
    [Trait("Category", "SqlInfrastructure")]
    public async Task WriteAsync_ConcurrentEventsPersistWithIsolatedCorrelationIds()
    {
        var connectionString = ConnectionString;
        var service = CreateService(connectionString);
        var events = Enumerable.Range(0, 4)
            .Select(_ => CreateEvent(Guid.NewGuid()))
            .ToArray();

        var ids = await Task.WhenAll(events.Select(auditEvent => service.WriteAsync(auditEvent)));

        Assert.All(ids, id => Assert.True(id > 0));
        Assert.Equal(ids.Length, ids.Distinct().Count());

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        for (var index = 0; index < ids.Length; index++)
        {
            await using var command = new SqlCommand("SELECT CorrelationId FROM Audit.AuditEvents WHERE AuditEventId = @id", connection);
            command.Parameters.Add("@id", System.Data.SqlDbType.BigInt).Value = ids[index]!.Value;
            Assert.Equal(events[index].CorrelationId, Assert.IsType<Guid>(await command.ExecuteScalarAsync()));
        }
    }

    private sealed class ForbiddenAuditOptions : IOptions<AuditOptions>
    {
        public AuditOptions Value => throw new InvalidOperationException("Invalid events must not access database configuration.");
    }

    private static SqlAuditEventService CreateService(string connectionString) => new(
        Options.Create(new AuditOptions
        {
            ConnectionString = connectionString,
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
