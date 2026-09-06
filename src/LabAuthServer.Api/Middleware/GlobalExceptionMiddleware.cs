using System.Text.Json;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Api.Requests;
using Microsoft.AspNetCore.Mvc;

namespace LabAuthServer.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            var correlation = CorrelationContext.Get(context);
            _logger.LogError("Unhandled request exception for {CorrelationId} at {Endpoint}.", correlation.CorrelationId, context.Request.Path);

            var audit = context.RequestServices.GetService<IAuditEventService>();
            if (audit is not null)
            {
                try
                {
                    await audit.WriteAsync(new AuditEvent
                    {
                        EventTypeCode = AuditEventTypes.UnhandledException,
                        CorrelationId = correlation.CorrelationId,
                        RequestId = correlation.RequestId,
                        Endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value,
                        HttpMethod = context.Request.Method,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Success = false,
                        ServerName = Environment.MachineName,
                        ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
                        DetailsJson = JsonSerializer.Serialize(new { exceptionCategory = "Unhandled" })
                    }, context.RequestAborted).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Unhandled exception audit persistence failed for {CorrelationId}.", correlation.CorrelationId);
                }
            }

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "The request could not be completed.",
                    Extensions = { ["correlationId"] = correlation.CorrelationId.ToString("D") }
                }, context.RequestAborted).ConfigureAwait(false);
            }
        }
    }
}
