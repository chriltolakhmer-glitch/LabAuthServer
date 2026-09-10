namespace LabAuthServer.Api.Requests;

public static class IngressSizePolicy
{
    public const int MaximumLoginBodyBytes = 8192;
    public const int MaximumAggregateHeaderBytes = 16128;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestBodyLimitAttribute(int maximumBytes) : Attribute
{
    public int MaximumBytes { get; } = maximumBytes > 0
        ? maximumBytes
        : throw new ArgumentOutOfRangeException(nameof(maximumBytes));
}