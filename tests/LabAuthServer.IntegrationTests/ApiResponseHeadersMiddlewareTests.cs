using LabAuthServer.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace LabAuthServer.IntegrationTests;

public sealed class ApiResponseHeadersMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/auth/login", true)]
    [InlineData("/API/v1/health", true)]
    [InlineData("/api/missing", true)]
    [InlineData("/public/logo.svg", false)]
    [InlineData("/apiculture", false)]
    public async Task Policy_OwnsOnlyApiHeadersAndPreservesResponse(string path, bool applies)
    {
        using var host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().Configure(app =>
        {
            app.UseMiddleware<ApiResponseHeadersMiddleware>();
            app.Run(context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers.WWWAuthenticate = "Bearer";
                context.Response.Headers["X-Correlation-ID"] = "original-correlation";
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "public, max-age=600";
                    context.Response.Headers.XContentTypeOptions = "downstream-value";
                    context.Response.Headers.XFrameOptions = "SAMEORIGIN";
                    return Task.CompletedTask;
                });
                return context.Response.WriteAsync("{\"status\":401}");
            });
        })).Build();
        await host.StartAsync();
        using var client = host.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal("{\"status\":401}", await response.Content.ReadAsStringAsync());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal("original-correlation", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(applies ? "no-store" : "public, max-age=600", response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal(applies ? "nosniff" : "downstream-value", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(applies ? "DENY" : "SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }
}
