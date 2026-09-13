using LabAuthServer.Application.Constants;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabAuthServer.Api.Requests;

namespace LabAuthServer.Api.Controllers;

[ApiController]
[Route("api/v1/protected")]
[Authorize(Policy = AuthorizationPolicies.RequireReader)]
public sealed class ProtectedController : ControllerBase
{
    private readonly IAuditEventService? _auditEventService;
    private readonly ILogger<ProtectedController> _logger;

    public ProtectedController(
        IAuditEventService? auditEventService = null,
        ILogger<ProtectedController>? logger = null)
    {
        _auditEventService = auditEventService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedController>.Instance;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await RecordGrantedAsync().ConfigureAwait(false);
        return Ok(new
        {
            Authenticated = User.Identity?.IsAuthenticated == true,
            User = User.FindFirst("sub")?.Value ?? User.Identity?.Name,
            Message = "Protected resource access granted"
        });
    }

    private async Task RecordGrantedAsync()
    {
        if (_auditEventService is null)
            return;

        try
        {
            var correlation = CorrelationContext.Get(HttpContext);
            var subject = User.FindFirst("sub")?.Value;
            var identityOmitted = subject is { Length: > AuditEventValidator.MaximumIdentityLength };
            await _auditEventService.WriteAsync(new AuditEvent
            {
                EventTypeCode = AuditEventTypes.AccessGranted,
                CorrelationId = correlation.CorrelationId,
                RequestId = correlation.RequestId,
                Username = identityOmitted ? null : subject,
                Subject = identityOmitted ? null : subject,
                Role = User.FindFirst("role")?.Value,
                Endpoint = "/api/v1/protected",
                HttpMethod = Request.Method,
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ServerName = Environment.MachineName,
                ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                DetailsJson = identityOmitted ? "{\"identityOmitted\":true}" : null
            }, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _logger.LogWarning("Audit event could not be persisted for an authorized request.");
        }
    }
}