using LabAuthServer.Api.Requests;

namespace LabAuthServer.Api.Middleware;

public sealed class RequestBodySizeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var limit = context.GetEndpoint()?.Metadata.GetMetadata<RequestBodyLimitAttribute>()?.MaximumBytes;
        if (limit is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (context.Request.ContentLength > limit.Value)
        {
            Reject(context);
            return;
        }

        var bufferedBody = new MemoryStream(limit.Value + 1);
        var readBuffer = new byte[Math.Min(4096, limit.Value + 1)];

        while (true)
        {
            var remaining = limit.Value + 1 - bufferedBody.Length;
            if (remaining <= 0)
            {
                Reject(context);
                return;
            }

            var read = await context.Request.Body.ReadAsync(
                readBuffer.AsMemory(0, (int)Math.Min(readBuffer.Length, remaining)),
                context.RequestAborted).ConfigureAwait(false);
            if (read == 0)
                break;

            await bufferedBody.WriteAsync(readBuffer.AsMemory(0, read), context.RequestAborted)
                .ConfigureAwait(false);
            if (bufferedBody.Length > limit.Value)
            {
                Reject(context);
                return;
            }
        }

        bufferedBody.Position = 0;
        context.Request.Body = bufferedBody;
        await next(context).ConfigureAwait(false);
    }

    private static void Reject(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
    }
}