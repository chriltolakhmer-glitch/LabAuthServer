using LabAuthServer.Application.Services;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabAuthServer.Api.Requests;
using LabAuthServer.Application.Auditing;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class JwtSizeBoundaryTests(JwtSizeApiFactory factory) : IClassFixture<JwtSizeApiFactory>
{
    [Fact]
    public async Task Login_ProducesUsable4096BitToken_AndHealthRemainsAvailable()
    {
        using var client = CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "reader@example.test", password = "synthetic-test-input" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.Equal(683, token.AccessToken.Split('.')[2].Length);
        using var request = BearerRequest(token.AccessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var health = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task MaximumIssuerPayload_WithEscapedKid_FitsApplicationEnvelope()
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<TokenOptions>>().Value;
        Assert.Equal(7680, options.MaximumTokenSize);
        var input = new TokenIssuanceRequest { Subject = "reader", Roles = ["Reader"], Scopes = [new string('a', 4096), "b"] };
        var initial = await issuer.IssueAsync(input);
        var payloadSize = Decode(initial.AccessToken.Split('.')[1]).Length;
        var lastScope = new string('b', 1 + 7680 - payloadSize);
        var largest = await issuer.IssueAsync(input with { Scopes = [new string('a', 4096), lastScope] });
        Assert.Equal(7680, Decode(largest.AccessToken.Split('.')[1]).Length);
        Assert.Equal(804, Decode(largest.AccessToken.Split('.')[0]).Length);
        Assert.Equal(11997, largest.AccessToken.Length);
        using var client = CreateClient();
        using var request = BearerRequest(largest.AccessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => issuer.IssueAsync(input with
        {
            Scopes = [new string('a', 4096), lastScope + "x"]
        }));
    }

    [Theory]
    [InlineData(12287, 0)]
    [InlineData(12288, 0)]
    [InlineData(12288, 56)]
    [InlineData(12288, 57)]
    public async Task OtherwiseValidToken_AtEncodedAndHeaderBoundaries_IsAccepted(int tokenLength, int trailingSpaces)
    {
        var token = CreateSignedTokenOfLength(tokenLength);
        if (trailingSpaces > 0)
        {
            // HttpClient normalizes trailing header whitespace. Supply the raw
            // value through TestServer to exercise the full application pipeline.
            var result = await factory.Server.SendAsync(context =>
            {
                context.Request.Method = "GET";
                context.Request.Scheme = "https";
            context.Request.Headers.Host = "DC01.lab.local";
                context.Request.Path = "/api/v1/protected";
                context.Request.Headers.Authorization = "Bearer " + token + new string(' ', trailingSpaces);
            });
            Assert.Equal(7 + tokenLength + trailingSpaces, Encoding.UTF8.GetByteCount(result.Request.Headers.Authorization.ToString()));
            Assert.Equal(200, result.Response.StatusCode);
            return;
        }
        using var request = BearerRequest(token + new string(' ', trailingSpaces));
        using var client = CreateClient();
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/protected", 12289, 401)]
    [InlineData("/api/v1/auth/login", 12289, 401)]
    [InlineData("/api/v1/protected", 12346, 431)]
    [InlineData("/api/v1/auth/login", 12346, 431)]
    public async Task OversizedInput_RejectsBeforeAuthenticationOrKeyAccess(string path, int tokenLength, int status)
    {
        using var client = CreateClient();
        var validationCalls = factory.ValidationCalls;
        var authenticationCalls = factory.AuthenticationCalls;
        var auditCalls = factory.AuditCalls;
        using var request = new HttpRequestMessage(path.EndsWith("login", StringComparison.Ordinal) ? HttpMethod.Post : HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + new string('a', tokenLength));
        request.Content = JsonContent.Create(new { username = "reader@example.test", password = "synthetic-test-input" });
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(validationCalls, factory.ValidationCalls);
        Assert.Equal(authenticationCalls, factory.AuthenticationCalls);
        Assert.Equal(auditCalls, factory.AuditCalls);
        Assert.True(Guid.TryParseExact(response.Headers.GetValues("X-Correlation-ID").Single(), "D", out _));
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void BearerHandler_UsesApprovedEncodedLimit()
    {
        var options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.All(options.TokenHandlers, handler => Assert.Equal(12288, handler.MaximumTokenSizeInBytes));
    }

    [Fact]
    public void InconsistentPayloadOverride_FailsOptionsValidation()
    {
        using var invalidFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<TokenOptions>(options => options.MaximumTokenSize = 16384)));
        Assert.Throws<OptionsValidationException>(() => invalidFactory.CreateClient());
    }

    [Theory]
    [InlineData(12289, 0, 401)]
    [InlineData(12288, 58, 431)]
    public async Task OtherwiseValidOversizedToken_RejectsWithoutDownstreamWork(int tokenLength, int spaces, int status)
    {
        var token = CreateSignedTokenOfLength(tokenLength);
        var validationCalls = factory.ValidationCalls;
        var authenticationCalls = factory.AuthenticationCalls;
        var auditCalls = factory.AuditCalls;
        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Headers.Host = "DC01.lab.local";
            context.Request.Path = "/api/v1/protected";
            context.Request.Headers.Authorization = "Bearer " + new string(' ', spaces) + token;
        });
        Assert.Equal(status, result.Response.StatusCode);
        Assert.Equal(status == 401 ? "Bearer" : string.Empty, result.Response.Headers.WWWAuthenticate.ToString());
        Assert.Equal(validationCalls, factory.ValidationCalls);
        Assert.Equal(authenticationCalls, factory.AuthenticationCalls);
        Assert.Equal(auditCalls, factory.AuditCalls);
    }

    [Fact]
    public async Task RepeatedAuthorization_RejectsBeforeDownstreamWorkIncludingSeparator()
    {
        var validationCalls = factory.ValidationCalls;
        var authenticationCalls = factory.AuthenticationCalls;
        var auditCalls = factory.AuditCalls;
        var result = await factory.Server.SendAsync(context =>
        {
            context.Request.Scheme = "https";
            context.Request.Headers.Host = "DC01.lab.local";
            context.Request.Path = "/api/v1/protected";
            context.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues(
                [new string('a', 6176), new string('b', 6176)]);
        });
        Assert.Equal(431, result.Response.StatusCode);
        Assert.Equal(validationCalls, factory.ValidationCalls);
        Assert.Equal(authenticationCalls, factory.AuthenticationCalls);
        Assert.Equal(auditCalls, factory.AuditCalls);
    }

    [Fact]
    public async Task UnicodeIdentityAndScope_RoundTrip_AndTamperingStillFails()
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var subject = "reader-\u00e9@size-tests.example";
        var scopeValue = "reports.\u0e2d\u0e48\u0e32\u0e19\"\\<&>";
        var token = await issuer.IssueAsync(new TokenIssuanceRequest { Subject = subject, Roles = ["Reader"], Scopes = [scopeValue] });
        using var payload = JsonDocument.Parse(Decode(token.AccessToken.Split('.')[1]));
        Assert.Equal(subject, payload.RootElement.GetProperty("sub").GetString());
        Assert.Equal(scopeValue, payload.RootElement.GetProperty("scope")[0].GetString());
        Assert.Equal("Reader", payload.RootElement.GetProperty("role").GetString());
        using var client = CreateClient();
        using var request = BearerRequest(token.AccessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var parts = token.AccessToken.Split('.');
        var signature = Decode(parts[2]);
        signature[0] ^= 1;
        using var tampered = BearerRequest(parts[0] + "." + parts[1] + "." + Encode(signature));
        using var rejected = await client.SendAsync(tampered);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        await Assert.ThrowsAsync<ArgumentException>(() => issuer.IssueAsync(new TokenIssuanceRequest
        {
            Subject = subject, Roles = ["R\u00e9ader"], Scopes = [scopeValue]
        }));
    }

    [Theory]
    [InlineData("Reader", 200)]
    [InlineData("Operator", 403)]
    [InlineData("Administrator", 403)]
    public async Task MaximumLoginUnicodeIdentity_PreservesApprovedRoleBehavior(string role, int expectedStatus)
    {
        using var scope = factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ITokenService>();
        const string suffix = "@size-tests.example";
        var subject = new string('\u0e01', 1024 - suffix.Length) + suffix;
        var token = await issuer.IssueAsync(new TokenIssuanceRequest { Subject = subject, Roles = [role], Scopes = [] });
        Assert.True(token.AccessToken.Length <= JwtRequestSizePolicy.MaximumEncodedJwtSize);
        using var payload = JsonDocument.Parse(Decode(token.AccessToken.Split('.')[1]));
        Assert.Equal(subject, payload.RootElement.GetProperty("sub").GetString());
        using var client = CreateClient();
        using var request = BearerRequest(token.AccessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://DC01.lab.local") });

    private static HttpRequestMessage BearerRequest(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/protected");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        return request;
    }

    private string CreateSignedTokenOfLength(int target)
    {
        // The transport margin allows otherwise valid inbound tokens larger
        // than our issuance payload budget. Do not enlarge the issuer for this test.
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = JwtSizeApiFactory.KeyId }));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        for (var padding = 7000; padding < 9000; padding++)
        {
            var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new
            {
                iss = JwtSizeApiFactory.Issuer, aud = "size-tests", sub = "reader", jti = "boundary-test",
                iat = now, nbf = now, exp = now + 600, role = "Reader", padding = new string('a', padding)
            }));
            if (header.Length + payload.Length + 683 + 2 != target) continue;
            var input = header + "." + payload;
            var signature = factory.Key.SignData(Encoding.ASCII.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var token = input + "." + Encode(signature);
            Assert.Equal(target, token.Length);
            return token;
        }
        throw new InvalidOperationException("Unable to construct exact-boundary fixture.");
    }

    private static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Decode(string encoded) => Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
}

public sealed class JwtSizeApiFactory : WebApplicationFactory<Program>
{
    public JwtSizeApiFactory() => ClientOptions.BaseAddress = new Uri("https://DC01.lab.local");
    public const string Issuer = "https://size-tests.example";
    public static readonly string KeyId = new('\u00e9', 128);
    public RSA Key { get; } = RSA.Create(4096);
    public int ValidationCalls;
    public int AuthenticationCalls;
    public int AuditCalls;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.Configure<TokenOptions>(options =>
            {
                options.Issuer = Issuer;
                options.Audience = "size-tests";
                options.ActiveKeyId = KeyId;
                options.SigningCertificateStoreLocation = string.Empty;
                options.SigningCertificateStoreName = string.Empty;
                options.SigningCertificateThumbprint = string.Empty;
                options.PreviousSigningCertificateThumbprint = string.Empty;
                options.PreviousKeyId = string.Empty;
                options.SigningKeyStoreReference = "runtime-test";
            });
            services.RemoveAll<IProtectedSigningKeyProvider>();
            services.AddSingleton<IProtectedSigningKeyProvider>(sp => new ObservedKeyProvider(this,
                new ProtectedSigningKeyProvider(sp.GetRequiredService<IOptions<TokenOptions>>(),
                    new[] { new KeyValuePair<string, RSA>(KeyId, Key) })));
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService>(new AuthenticationStub(this));
            services.RemoveAll<ILdapService>();
            services.AddSingleton<ILdapService>(new GroupsStub());
            services.RemoveAll<IAuditEventService>();
            services.AddSingleton<IAuditEventService>(new AuditStub(this));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Key.Dispose();
    }

    private sealed class ObservedKeyProvider(JwtSizeApiFactory owner, IProtectedSigningKeyProvider inner) : IProtectedSigningKeyProvider
    {
        public ValueTask<SigningKeyMaterial> GetActiveKeyAsync(CancellationToken cancellationToken = default) => inner.GetActiveKeyAsync(cancellationToken);
        public ValueTask<ValidationKeyMaterial> GetValidationKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref owner.ValidationCalls);
            return inner.GetValidationKeyAsync(keyIdentifier, cancellationToken);
        }
    }

    private sealed class AuthenticationStub(JwtSizeApiFactory owner) : IAuthenticationService
    {
        public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            Interlocked.Increment(ref owner.AuthenticationCalls);
            return Task.FromResult(AuthenticationResult.Succeeded(username ));
        }
    }

    private sealed class GroupsStub : ILdapService
    {
        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GroupLookupResult> GetUserGroupsAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
            => Task.FromResult(GroupLookupResult.Succeeded(["GG-APP-USER"]));
    }

    private sealed class AuditStub(JwtSizeApiFactory owner) : IAuditEventService
    {
        public Task<long?> WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref owner.AuditCalls);
            return Task.FromResult<long?>(1);
        }
    }
}
