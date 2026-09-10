using LabAuthServer.Api.Requests;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;

namespace LabAuthServer.Api.Middleware;

public sealed class AuthorizationAuditMiddleware : IMiddleware
{
    private readonly IAuditEventService _auditEventService;
    private readonly ILogger<AuthorizationAuditMiddleware> _logger;

    public AuthorizationAuditMiddleware(
        IAuditEventService auditEventService,
        ILogger<AuthorizationAuditMiddleware> logger)
    {
        _auditEventService = auditEventService ?? throw new ArgumentNullException(nameof(auditEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context).ConfigureAwait(false);

        if (context.Response.StatusCode != StatusCodes.Status403Forbidden)
            return;

        try
        {
            var correlation = CorrelationContext.Get(context);
            var subject = context.User.FindFirst("sub")?.Value;
            var identityOmitted = subject is { Length: > 256 };
            await _auditEventService.WriteAsync(new AuditEvent
            {
                EventTypeCode = AuditEventTypes.AccessDenied,
                CorrelationId = correlation.CorrelationId,
                RequestId = correlation.RequestId,
                Username = identityOmitted ? null : subject,
                Subject = identityOmitted ? null : subject,
                Endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value,
                HttpMethod = context.Request.Method,
                StatusCode = StatusCodes.Status403Forbidden,
                Success = false,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                ServerName = Environment.MachineName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                DetailsJson = identityOmitted
                    ? "{\"policy\":\"authorization\",\"identityOmitted\":true}"
                    : "{\"policy\":\"authorization\"}"
            }, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _logger.LogWarning("Audit event could not be persisted for a denied request.");
        }
    }
}
