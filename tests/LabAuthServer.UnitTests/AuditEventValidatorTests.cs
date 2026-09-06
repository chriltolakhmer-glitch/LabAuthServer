using LabAuthServer.Application.Auditing;

namespace LabAuthServer.UnitTests;

public sealed class AuditEventValidatorTests
{
    [Fact]
    public void Validate_WithApprovedEventAndMetadata_ReturnsNoFailures()
    {
        var failures = AuditEventValidator.Validate(CreateEvent());

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_RejectsUnknownEventRoleStatusAndCorrelation()
    {
        var auditEvent = CreateEvent() with
        {
            EventTypeCode = "UNKNOWN",
            CorrelationId = Guid.Empty,
            Role = "Owner",
            StatusCode = 700
        };

        var failures = AuditEventValidator.Validate(auditEvent);

        Assert.Contains(failures, failure => failure.Contains("event type", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("correlation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("status", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("{\"password\":\"secret\"}")]
    [InlineData("{\"nested\":{\"accessToken\":\"token\"}}")]
    [InlineData("not-json")]
    public void Validate_RejectsSensitiveOrInvalidDetails(string detailsJson)
    {
        var failures = AuditEventValidator.Validate(CreateEvent() with { DetailsJson = detailsJson });

        Assert.NotEmpty(failures);
    }

    [Fact]
    public void Validate_RejectsOverlongValuesAndInvalidIp()
    {
        var failures = AuditEventValidator.Validate(CreateEvent() with
        {
            Username = new string('a', 257),
            ClientIp = "not-an-ip"
        });

        Assert.Contains(failures, failure => failure.Contains("Username", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("IP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsSensitivePropertiesNestedInArrays()
    {
        var failures = AuditEventValidator.Validate(CreateEvent() with
        {
            DetailsJson = "{\"items\":[{\"privateKey\":\"placeholder\"}]}"
        });

        Assert.Contains(failures, failure => failure.Contains("prohibited", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsSensitivePropertiesInDeeplyNestedArrays()
    {
        var failures = AuditEventValidator.Validate(CreateEvent() with
        {
            DetailsJson = "{\"items\":[[[{\"password\":\"placeholder\"}]]]}"
        });

        Assert.Contains(failures, failure => failure.Contains("prohibited", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AcceptsStatusRangeBoundaries()
    {
        Assert.Empty(AuditEventValidator.Validate(CreateEvent() with { StatusCode = 100 }));
        Assert.Empty(AuditEventValidator.Validate(CreateEvent() with { StatusCode = 599 }));
    }

    [Fact]
    public void Validate_RejectsNullEvent()
    {
        Assert.Throws<ArgumentNullException>(() => AuditEventValidator.Validate(null!));
    }

    private static AuditEvent CreateEvent() => new()
    {
        EventTypeCode = AuditEventTypes.LoginFailure,
        CorrelationId = Guid.NewGuid(),
        Username = "alice@lab.local",
        Subject = "alice@lab.local",
        Role = "Reader",
        Endpoint = "/api/v1/auth/login",
        HttpMethod = "POST",
        StatusCode = 401,
        Success = false,
        ClientIp = "127.0.0.1",
        DetailsJson = "{\"failureCategory\":\"InvalidCredentials\"}"
    };
}
