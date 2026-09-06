namespace LabAuthServer.Application.Auditing;

public sealed record AuditEvent
{
    public required string EventTypeCode { get; init; }
    public DateTime? EventTimeUtc { get; init; }
    public required Guid CorrelationId { get; init; }
    public string? RequestId { get; init; }
    public string? Username { get; init; }
    public string? Subject { get; init; }
    public string? Role { get; init; }
    public string? Endpoint { get; init; }
    public string? HttpMethod { get; init; }
    public short? StatusCode { get; init; }
    public bool? Success { get; init; }
    public string? ClientIp { get; init; }
    public string? ServerName { get; init; }
    public string? ApplicationVersion { get; init; }
    public string? DetailsJson { get; init; }
}
