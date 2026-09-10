using System.Text;
using LabAuthServer.Api.Middleware;
using LabAuthServer.Api.Requests;
using Microsoft.AspNetCore.Http;

namespace LabAuthServer.UnitTests;

public sealed class RequestBodySizeMiddlewareTests
{
    [Fact]
    public async Task ExactLimit_ForwardsAndPreservesBody()
    {
        var payload = Encoding.UTF8.GetBytes(new string('a', IngressSizePolicy.MaximumLoginBodyBytes));
        var context = CreateContext(payload);
        byte[]? observed = null;

        await new RequestBodySizeMiddleware(async nextContext =>
        {
            using var copy = new MemoryStream();
            await nextContext.Request.Body.CopyToAsync(copy);
            observed = copy.ToArray();
        }).InvokeAsync(context);

        Assert.Equal(payload, observed);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task OneByteOverLimit_RejectsBeforeNext()
    {
        var context = CreateContext(new byte[IngressSizePolicy.MaximumLoginBodyBytes + 1]);
        var called = false;

        await new RequestBodySizeMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.False(called);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
    }

    [Fact]
    public async Task AbsentContentLength_RejectsStreamedBodyOverLimit()
    {
        var context = CreateContext(new byte[IngressSizePolicy.MaximumLoginBodyBytes + 1]);
        context.Request.ContentLength = null;
        var called = false;

        await new RequestBodySizeMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.False(called);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(byte[] payload)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(payload, writable: false);
        context.Request.ContentLength = payload.Length;
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new RequestBodyLimitAttribute(IngressSizePolicy.MaximumLoginBodyBytes)),
            "login"));
        return context;
    }
}