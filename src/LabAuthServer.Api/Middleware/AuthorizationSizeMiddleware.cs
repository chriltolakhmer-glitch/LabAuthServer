using System.Text;
using LabAuthServer.Api.Requests;

namespace LabAuthServer.Api.Middleware;

public sealed class AuthorizationSizeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var values = context.Request.Headers.Authorization;
        long byteCount = 0;
        for (var index = 0; index < values.Count; index++)
        {
            // StringValues.ToString joins multiple values with commas. Count
            // before joining so excessive input cannot cause another allocation.
            byteCount += (index == 0 ? 0 : 1) + Encoding.UTF8.GetByteCount(values[index] ?? string.Empty);
            if (byteCount > JwtRequestSizePolicy.MaximumAuthorizationHeaderSize)
            {
                context.Response.StatusCode = StatusCodes.Status431RequestHeaderFieldsTooLarge;
                return;
            }
        }

        if (IsOversizedBearer(values.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsOversizedBearer(string authorization)
    {
        // Match the existing bearer handler's scheme and whitespace handling.
        var value = authorization.AsSpan();
        return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
               Encoding.UTF8.GetByteCount(value[7..].Trim()) > JwtRequestSizePolicy.MaximumEncodedJwtSize;
    }
}
