using LabAuthServer.Api.Middleware;
using LabAuthServer.Api.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LabAuthServer.UnitTests;

public sealed class AuthorizationSizeMiddlewareTests
{
    [Theory]
    [InlineData(12351, true)]
    [InlineData(12352, true)]
    [InlineData(12353, false)]
    public async Task HeaderValue_EnforcesExactBoundary(int bytes, bool nextExpected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = new string('a', bytes);
        var called = false;
        await new AuthorizationSizeMiddleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.Equal(nextExpected, called);
        if (!nextExpected) Assert.Equal(431, context.Response.StatusCode);
    }

    [Fact]
    public async Task HeaderValue_CountsUtf8BytesRatherThanCharacters()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = new string('\u00e9', 6177);
        await new AuthorizationSizeMiddleware(_ => throw new InvalidOperationException("Must reject before next.")).InvokeAsync(context);
        Assert.Equal(431, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(6174, true)]
    [InlineData(6175, true)]
    [InlineData(6176, false)]
    public async Task MultipleHeaderValues_CountSeparatorAtBoundary(int secondValueBytes, bool nextExpected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = new StringValues([new string('a', 6176), new string('b', secondValueBytes)]);
        var called = false;
        await new AuthorizationSizeMiddleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.Equal(nextExpected, called);
        if (!nextExpected) Assert.Equal(431, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(12287, true)]
    [InlineData(12288, true)]
    [InlineData(12289, false)]
    public async Task BearerValue_EnforcesExactBoundaryAfterExistingTrimming(int bytes, bool nextExpected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "bEaReR  " + new string('a', bytes) + " ";
        var called = false;
        await new AuthorizationSizeMiddleware(_ => { called = true; return Task.CompletedTask; }).InvokeAsync(context);
        Assert.Equal(nextExpected, called);
        if (!nextExpected)
        {
            Assert.Equal(401, context.Response.StatusCode);
            Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);
        }
    }

    [Theory]
    [InlineData(7680, true)]
    [InlineData(7898, true)]
    [InlineData(7899, false)]
    [InlineData(16384, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(0, false)]
    public void PayloadBudget_MustFitFixedTransportEnvelope(int payloadBytes, bool expected)
        => Assert.Equal(expected, JwtRequestSizePolicy.SupportsPayloadSize(payloadBytes));
}
