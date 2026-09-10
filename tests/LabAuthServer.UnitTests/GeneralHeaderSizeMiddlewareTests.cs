using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using LabAuthServer.Api.Middleware;
using LabAuthServer.Api.Requests;

namespace LabAuthServer.UnitTests;

public sealed class GeneralHeaderSizeMiddlewareTests
{
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task AggregateEnvelope_EnforcesExactBoundary(int adjustment, bool nextExpected)
    {
        var context = CreateContext();
        var baseSize = GeneralHeaderSizeMiddleware.CalculateEnvelopeSize(context);
        var valueBytes = checked(IngressSizePolicy.MaximumAggregateHeaderBytes - (int)baseSize - Encoding.UTF8.GetByteCount("X-Test") - 4 + adjustment);
        context.Request.Headers["X-Test"] = new string('a', valueBytes);
        var called = false;

        await new GeneralHeaderSizeMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.Equal(nextExpected, called);
        Assert.Equal(nextExpected ? StatusCodes.Status200OK : StatusCodes.Status431RequestHeaderFieldsTooLarge, context.Response.StatusCode);
    }

    [Fact]
    public async Task DuplicateValues_CountSeparatorAndBothValues()
    {
        var context = CreateContext();
        context.Request.Headers["X-Test"] = new StringValues(["a", "b"]);

        var size = GeneralHeaderSizeMiddleware.CalculateEnvelopeSize(context);

        Assert.Equal(
            Encoding.UTF8.GetByteCount("/api/v1/protected") +
            Encoding.UTF8.GetByteCount("X-Test") + 4 + 3,
            size);
    }

    [Fact]
    public async Task AuthorizationValues_AreCountedOnceByGeneralPolicy()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = new StringValues(["first", "second"]);
        var called = false;

        await new GeneralHeaderSizeMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.True(called);
    Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/protected";
        return context;
    }
}