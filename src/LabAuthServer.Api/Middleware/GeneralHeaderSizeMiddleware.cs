using System.Text;
using LabAuthServer.Api.Requests;

namespace LabAuthServer.Api.Middleware;

public sealed class GeneralHeaderSizeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (CalculateEnvelopeSize(context) > IngressSizePolicy.MaximumAggregateHeaderBytes)
        {
            context.Response.StatusCode = StatusCodes.Status431RequestHeaderFieldsTooLarge;
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    internal static long CalculateEnvelopeSize(HttpContext context)
    {
        long byteCount = Encoding.UTF8.GetByteCount(context.Request.PathBase + context.Request.Path + context.Request.QueryString);

        foreach (var header in context.Request.Headers)
        {
            byteCount += Encoding.UTF8.GetByteCount(header.Key) + 4;
            for (var index = 0; index < header.Value.Count; index++)
            {
                if (index > 0)
                    byteCount++;

                byteCount += Encoding.UTF8.GetByteCount(header.Value[index] ?? string.Empty);
            }
        }

        return byteCount;
    }
}