using LabAuthServer.Api.Middleware;
using LabAuthServer.Api.Requests;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabAuthServer.UnitTests;

public sealed class CorrelationAndExceptionMiddlewareTests
{
    [Fact]
    public async Task CorrelationMiddleware_GeneratesAndReturnsCanonicalCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationMiddleware(
            next: async httpContext => await httpContext.Response.StartAsync(),
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.True(Guid.TryParseExact(value, "D", out _));
    }

    [Fact]
    public async Task CorrelationMiddleware_ReplacesInvalidCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "not-a-guid";
        var middleware = new CorrelationMiddleware(
            next: async httpContext => await httpContext.Response.StartAsync(),
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.NotEqual("not-a-guid", value);
        Assert.True(Guid.TryParseExact(value, "D", out _));
    }

    [Fact]
    public async Task CorrelationMiddleware_PropagatesCanonicalClientCorrelationIdAndRequestId()
    {
        var supplied = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = supplied;
        context.TraceIdentifier = "request-42";
        CorrelationContext? captured = null;
        var middleware = new CorrelationMiddleware(
            next: httpContext =>
            {
                captured = CorrelationContext.Get(httpContext);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.NotNull(captured);
        Assert.Equal(Guid.Parse(supplied), captured.CorrelationId);
        Assert.Equal("request-42", captured.RequestId);
        Assert.Equal(supplied, context.Response.Headers["X-Correlation-ID"].ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa-extra")]
    public async Task CorrelationMiddleware_ReplacesMalformedOrOversizedCorrelationId(string supplied)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = supplied;
        var middleware = new CorrelationMiddleware(
            next: _ => Task.CompletedTask,
            NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var value = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.NotEqual(supplied, value);
        Assert.True(Guid.TryParseExact(value, "D", out _));
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_ReturnsSafeCorrelatedResponseWhenAuditFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuditEventService, FailingAuditEventService>();
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Items[CorrelationContext.ItemKey] =
            new CorrelationContext(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "request-1");
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("database password and access token"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.DoesNotContain("database password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GlobalExceptionMiddleware_PreservesCorrelationAndWritesMinimizedAuditEvent()
    {
        var audit = new CapturingAuditEventService();
        var services = new ServiceCollection();
        services.AddSingleton<IAuditEventService>(audit);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/failing";
        context.Response.Body = new MemoryStream();
        context.Items[CorrelationContext.ItemKey] =
            new CorrelationContext(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "request-2");
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("password=placeholder token=placeholder"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(audit.Event);
        Assert.Equal(AuditEventTypes.UnhandledException, audit.Event.EventTypeCode);
        Assert.Equal(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), audit.Event.CorrelationId);
        Assert.Equal((short?)StatusCodes.Status500InternalServerError, audit.Event.StatusCode);
        Assert.DoesNotContain("password", audit.Event.DetailsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", audit.Event.DetailsJson, StringComparison.OrdinalIgnoreCase);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.DoesNotContain("placeholder", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cccccccc-cccc-cccc-cccc-cccccccccccc", body, StringComparison.Ordinal);
    }

    private sealed class CapturingAuditEventService : IAuditEventService
    {
        public AuditEvent Event { get; private set; } = null!;

        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Event = auditEvent;
            return Task.FromResult<long?>(1);
        }
    }

    private sealed class FailingAuditEventService : IAuditEventService
    {
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("audit unavailable");
    }
}
