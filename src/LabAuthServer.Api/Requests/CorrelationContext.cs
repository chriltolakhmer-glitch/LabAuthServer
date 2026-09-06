namespace LabAuthServer.Api.Requests;

public sealed record CorrelationContext(Guid CorrelationId, string RequestId)
{
    public const string ItemKey = "LabAuthServer.CorrelationContext";

    public static CorrelationContext Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items[ItemKey] as CorrelationContext
            ?? throw new InvalidOperationException("Correlation context is not available.");
    }
}
