using LabAuthServer.Api.Requests;

namespace LabAuthServer.Api.Middleware;

public sealed class CorrelationMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        var correlationId = Guid.TryParseExact(supplied, "D", out var parsed) &&
                            string.Equals(supplied, parsed.ToString("D"), StringComparison.OrdinalIgnoreCase)
            ? parsed
            : Guid.NewGuid();

        context.Items[CorrelationContext.ItemKey] = new CorrelationContext(correlationId, context.TraceIdentifier);
        context.Response.Headers[HeaderName] = correlationId.ToString("D");

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = context.TraceIdentifier
        });

        await _next(context).ConfigureAwait(false);
    }
}
