namespace LabAuthServer.Api.Middleware;

/// <summary>Owns response cache, MIME and framing policy for the JSON API.</summary>
public sealed class ApiResponseHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Include early rejections and fresh health results without imposing a
        // cache policy on future public resources outside the API path.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.OnStarting(() =>
            {
                // OnStarting callbacks run in reverse registration order. This
                // outer middleware assigns one final value, including on errors.
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers.XFrameOptions = "DENY";
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
