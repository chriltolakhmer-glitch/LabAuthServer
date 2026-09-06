namespace LabAuthServer.Infrastructure.Auditing;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 5;
}
