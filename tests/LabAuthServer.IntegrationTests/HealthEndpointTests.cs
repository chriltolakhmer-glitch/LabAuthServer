using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LabAuthServer.IntegrationTests;

public sealed class HealthEndpointTests(InfrastructureSafeApiFactory factory)
    : IClassFixture<InfrastructureSafeApiFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsHealthyResponse()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });

        var response = await client.GetAsync("/api/v1/health");
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(healthResponse);
        Assert.Equal("Healthy", healthResponse.Status);
        Assert.True(Guid.TryParseExact(
            response.Headers.GetValues("X-Correlation-ID").Single(),
            "D",
            out _));
    }

    [Fact]
    public async Task GetHealth_WithValidCorrelationId_PropagatesSameValue()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });
        const string correlationId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task GetHealth_ConcurrentRequestsReceiveDistinctCorrelationIds()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => client.GetAsync("/api/v1/health")));
        var correlationIds = responses
            .Select(response => response.Headers.GetValues("X-Correlation-ID").Single())
            .ToArray();

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(correlationIds.Length, correlationIds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(correlationIds, value => Assert.True(Guid.TryParseExact(value, "D", out _)));
    }

    [Fact]
    public async Task GetProtected_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });

        var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterFixedWindowIsExhausted_ReturnsTooManyRequests()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://DC01.lab.local")
        });

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/v1/auth/login", content);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var finalContent = new StringContent("{}", Encoding.UTF8, "application/json");
        var limitedResponse = await client.PostAsync("/api/v1/auth/login", finalContent);

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);

        var protectedResponse = await client.GetAsync("/api/v1/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    private sealed record HealthResponse(string Status);
}
