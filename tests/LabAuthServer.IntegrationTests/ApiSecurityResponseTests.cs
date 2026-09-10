using LabAuthServer.Application.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LabAuthServer.IntegrationTests;

public sealed class ApiSecurityResponseTests
{
    private static readonly WebApplicationFactoryClientOptions ClientOptions = new()
    {
        BaseAddress = new Uri("https://DC01.lab.local"), AllowAutoRedirect = false
    };

    [Fact]
    public async Task MalformedPort_CannotReachApplicationThroughRawTestServer()
    {
        // TestServer bypasses native HTTP syntax validation. The framework
        // HostString parser throws here, outside the explicit application pipeline.
        using var factory = new JwtSizeApiFactory();
        await Assert.ThrowsAsync<FormatException>(() => factory.Server.SendAsync(context =>
        {
            context.Request.Scheme = "https";
            context.Request.Path = "/api/v1/auth/login";
            context.Request.Headers.Host = "DC01.lab.local:invalid";
        }));
        Assert.Equal(0, factory.AuthenticationCalls);
        Assert.Equal(0, factory.ValidationCalls);
        Assert.Equal(0, factory.AuditCalls);
    }

    [Theory]
    [InlineData("DC01.lab.local", 200)]
    [InlineData("dc01.lab.local", 200)]
    [InlineData("DC01.lab.local:443", 200)]
    [InlineData("unapproved.example", 400)]
    [InlineData("127.0.0.1", 400)]
    [InlineData("192.0.2.1", 400)]
    [InlineData("bad host", 400)]
    [InlineData("DC01.lab.local,evil.example", 400)]
    [InlineData("", 400)]
    public async Task HostFiltering_EnforcesApprovedNameBeforeLogin(string host, int status)
    {
        using var factory = new JwtSizeApiFactory();
        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Scheme = "https";
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/auth/login";
            context.Request.Headers.Host = host;
            context.Request.Headers["X-Forwarded-Host"] = "DC01.lab.local";
            context.Request.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes("{\"username\":\"reader@example.test\",\"password\":\"synthetic\"}");
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);
        });

        Assert.Equal(status, result.Response.StatusCode);
        Assert.Equal(status == 200 ? 1 : 0, factory.AuthenticationCalls);
        if (status == 400)
        {
            Assert.Equal(0, factory.ValidationCalls);
            Assert.Equal(0, factory.AuditCalls);
            Assert.False(result.Response.Headers.ContainsKey("X-Correlation-ID"));
        }
    }

    // Phase 3.1: the application registers no forwarded-header middleware, so X-Forwarded-* and
    // X-Real-IP are inert. These tests are topology-independent: they assert only that supplying
    // the headers cannot change any security-relevant outcome, and encode no proxy address,
    // network range or hop count.
    [Theory]
    [InlineData("X-Forwarded-For", "10.10.10.10")]
    [InlineData("X-Forwarded-For", "10.10.10.10, 10.10.10.11")]
    [InlineData("X-Forwarded-Proto", "http")]
    [InlineData("X-Forwarded-Host", "unapproved.example")]
    [InlineData("X-Real-IP", "10.10.10.10")]
    public async Task ForwardedHeaders_CannotChangeApprovedHostOutcome(string name, string value)
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.TryAddWithoutValidation(name, value);
        request.Content = JsonContent.Create(new { username = "reader@example.test", password = "synthetic" });

        using var response = await client.SendAsync(request);

        // The real Host remains approved, so the request proceeds exactly as without the header.
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal(1, factory.AuthenticationCalls);
    }

    [Theory]
    [InlineData("https")]
    [InlineData("HTTPS")]
    public async Task SpoofedForwardedScheme_DoesNotMakePlainHttpAppearSecure(string forwardedScheme)
    {
        using var factory = new JwtSizeApiFactory();
        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Scheme = "http";
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/auth/login";
            context.Request.Headers.Host = "DC01.lab.local";
            context.Request.Headers["X-Forwarded-Proto"] = forwardedScheme;
            context.Request.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes("{\"username\":\"reader@example.test\",\"password\":\"synthetic\"}");
            context.Request.ContentLength = bytes.Length;
            context.Request.Body = new MemoryStream(bytes);
        });

        // Either HTTPS redirection or the controller's own IsHttps check applies; credentials
        // must never be processed based on a forwarded scheme claim.
        Assert.NotEqual(200, result.Response.StatusCode);
        Assert.Equal(0, factory.AuthenticationCalls);
    }

    // Phase 3 approved HSTS policy: max-age=31536000, no includeSubDomains, no preload.
    // The 200 health and 401 challenge paths are asserted through AssertPolicy below.
    [Fact]
    public async Task Hsts_IsEmittedOnHttpsHealthWithApprovedPolicy()
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(200, (int)response.StatusCode);
        AssertHsts(response);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
    }

    [Fact]
    public async Task Hsts_IsEmittedOnHttpsAuthenticationChallenge()
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/api/v1/protected");

        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal("Bearer", string.Join(",", response.Headers.WwwAuthenticate));
        AssertHsts(response);
    }

    [Fact]
    public async Task Hsts_IsNotEmittedOnPlainHttp()
    {
        using var factory = new JwtSizeApiFactory();
        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Scheme = "http";
            context.Request.Path = "/api/v1/health";
            context.Request.Headers.Host = "DC01.lab.local";
        });

        Assert.False(result.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_ExcludesIncludeSubDomainsAndPreload()
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/api/v1/health");

        var value = response.Headers.GetValues("Strict-Transport-Security").Single();
        Assert.Equal("max-age=31536000", value);
        Assert.DoesNotContain("includeSubDomains", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("preload", value, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AuthenticationFailureCategory.InvalidCredentials, 401)]
    [InlineData(AuthenticationFailureCategory.InvalidRequest, 400)]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, 503)]
    [InlineData(AuthenticationFailureCategory.Timeout, 504)]
    [InlineData(AuthenticationFailureCategory.Cancelled, 499)]
    [InlineData(AuthenticationFailureCategory.Configuration, 500)]
    [InlineData(AuthenticationFailureCategory.Unexpected, 500)]
    public async Task LoginFailure_PreservesProblemAndAddsHeaders(AuthenticationFailureCategory category, int status)
    {
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService>(new FailedAuthentication(category));
        }));
        using var client = configured.CreateClient(ClientOptions);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@example.test", password = "synthetic" });
        AssertPolicy(response, status);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(new DirectoryFailure(category, DirectoryFailureStage.UserBind, DirectoryFailureReason.UnexpectedFailure).SafeMessage, body.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(8191, 200)]
    [InlineData(8192, 200)]
    [InlineData(8193, 413)]
    public async Task LoginBodyBoundary_PreservesOutcomeAndResponsePolicy(int bytes, int status)
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        const string json = "{\"username\":\"reader@example.test\",\"password\":\"synthetic\"}";
        using var content = new StringContent(json + new string(' ', bytes - Encoding.UTF8.GetByteCount(json)), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/v1/auth/login", content);
        AssertPolicy(response, status);
        if (status == 413)
        {
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.Equal(0, factory.AuthenticationCalls);
        }
        else
        {
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.NotNull(token);
            Assert.False(string.IsNullOrEmpty(token.AccessToken));
        }
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Reader", 200)]
    [InlineData("Operator", 403)]
    public async Task ProtectedResponse_PreservesStatusChallengeAndBody(string? role, int status)
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        if (role is not null)
        {
            using var scope = factory.Services.CreateScope();
            var token = await scope.ServiceProvider.GetRequiredService<ITokenService>().IssueAsync(
                new TokenIssuanceRequest { Subject = "reader", Roles = [role], Scopes = [] });
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        }
        using var response = await client.GetAsync("/api/v1/protected");
        AssertPolicy(response, status);
        Assert.Equal(status == 401 ? "Bearer" : string.Empty, string.Join(",", response.Headers.WwwAuthenticate));
        if (status == 200)
        {
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("authenticated").GetBoolean());
            Assert.Equal("reader", body.RootElement.GetProperty("user").GetString());
        }
        else Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_UsesFreshLivenessPolicyAndPreservesCorrelation()
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        var correlation = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var response = await client.GetAsync("/api/v1/health");
        AssertPolicy(response, 200);
        Assert.Equal(correlation, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("{\"status\":\"Healthy\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidationAndLimiter_Preserve400Then429()
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        for (var attempt = 0; attempt < 11; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { });
            AssertPolicy(response, attempt < 10 ? 400 : 429);
            if (attempt < 10) Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            else Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(0, factory.AuthenticationCalls);
    }

    [Theory]
    [InlineData("Authorization", 12353)]
    [InlineData("X-Metadata", 16128)]
    public async Task HeaderLimits_KeepEmpty431AndNoDownstreamWork(string name, int length)
    {
        using var factory = new JwtSizeApiFactory();
        using var client = factory.CreateClient(ClientOptions);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/protected");
        request.Headers.TryAddWithoutValidation(name, new string('a', length));
        using var response = await client.SendAsync(request);
        AssertPolicy(response, 431);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        Assert.Equal(0, factory.ValidationCalls);
        Assert.Equal(0, factory.AuthenticationCalls);
    }

    [Fact]
    public async Task ProtectedApplicationException_ReceivesPolicyAndSafe500()
    {
        using var factory = new JwtSizeApiFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<MvcOptions>(options => options.Filters.Add(new ThrowingActionFilter()))));
        using var client = configured.CreateClient(ClientOptions);
        using var scope = configured.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<ITokenService>().IssueAsync(
            new TokenIssuanceRequest { Subject = "reader", Roles = ["Reader"], Scopes = [] });
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        using var response = await client.GetAsync("/api/v1/protected");
        AssertPolicy(response, 500);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("An unexpected error occurred.", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(response.Headers.GetValues("X-Correlation-ID").Single(), body.RootElement.GetProperty("correlationId").GetString());
    }

    private static void AssertPolicy(HttpResponseMessage response, int status)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(Guid.TryParseExact(response.Headers.GetValues("X-Correlation-ID").Single(), "D", out _));
        AssertHsts(response);
    }

    // Phase 3 approved HSTS policy: one year, no includeSubDomains, no preload.
    private static void AssertHsts(HttpResponseMessage response)
    {
        var value = response.Headers.GetValues("Strict-Transport-Security").Single();
        Assert.Equal("max-age=31536000", value);
        Assert.DoesNotContain("includeSubDomains", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("preload", value, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FailedAuthentication(AuthenticationFailureCategory category) : IAuthenticationService
    {
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
            => Task.FromResult(AuthenticationResult.Failed(new DirectoryFailure(category, LabAuthServer.Application.Enums.DirectoryFailureStage.UserBind, LabAuthServer.Application.Enums.DirectoryFailureReason.UnexpectedFailure)));
    }

    private sealed class ThrowingActionFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            => throw new InvalidOperationException("Synthetic application failure.");
    }
}
