using LabAuthServer.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace LabAuthServer.UnitTests;

public sealed class ApiResponseHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PropagatesRequestCancellationWithoutManufacturingResponse()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/login";
        context.RequestAborted = cancellation.Token;
        var middleware = new ApiResponseHeadersMiddleware(_ => Task.FromCanceled(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));

        Assert.False(context.Response.HasStarted);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_PreservesDownstreamExceptionForExistingExceptionHandler()
    {
        var expected = new InvalidOperationException("Synthetic failure.");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/protected";
        var middleware = new ApiResponseHeadersMiddleware(_ => throw expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(expected, actual);
        Assert.False(context.Response.HasStarted);
    }
}
