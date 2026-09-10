using System.Net;
using System.Text;

namespace LabAuthServer.IntegrationTests;

public sealed class IngressSizeBoundaryTests(JwtSizeApiFactory factory) : IClassFixture<JwtSizeApiFactory>
{
    [Fact]
    public async Task LoginBody_AtLimitReachesModelValidation()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{}" + new string(' ', 8190), Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.AuthenticationCalls);
        Assert.True(Guid.TryParseExact(response.Headers.GetValues("X-Correlation-ID").Single(), "D", out _));
    }

    [Fact]
    public async Task LoginBody_OneByteOverLimitRejectsBeforeAuthentication()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{}" + new string(' ', 8191), Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(0, factory.AuthenticationCalls);
        Assert.True(Guid.TryParseExact(response.Headers.GetValues("X-Correlation-ID").Single(), "D", out _));
    }

    [Fact]
    public async Task AggregateHeaders_RejectBeforeAuthentication()
    {
        var validationCalls = factory.ValidationCalls;
        var authenticationCalls = factory.AuthenticationCalls;

        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Headers.Host = "DC01.lab.local";
            context.Request.Path = "/api/v1/protected";
            context.Request.Headers["X-Metadata"] = new string('a', 16128);
        });

        Assert.Equal(431, (int)result.Response.StatusCode);
        Assert.Equal(validationCalls, factory.ValidationCalls);
        Assert.Equal(authenticationCalls, factory.AuthenticationCalls);
        Assert.True(Guid.TryParseExact(result.Response.Headers["X-Correlation-ID"].Single(), "D", out _));
    }
}