using LabAuthServer.Api.Controllers;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LabAuthServer.IntegrationTests;

public sealed class AuthenticationTests
{
    [Theory]
    [InlineData(AuthenticationFailureCategory.InvalidCredentials, 401)]
    [InlineData(AuthenticationFailureCategory.InvalidRequest, 400)]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, 503)]
    [InlineData(AuthenticationFailureCategory.Timeout, 504)]
    [InlineData(AuthenticationFailureCategory.Unexpected, 500)]
    public async Task Login_MapsAuthenticationFailuresToExpectedStatus(
        AuthenticationFailureCategory failureCategory,
        int expectedStatusCode)
    {
        var controller = CreateController(new FakeAuthenticationService
        {
            Result = new AuthenticationResult
            {
                IsAuthenticated = false,
                FailureCategory = failureCategory,
                ErrorMessage = GetMessage(failureCategory)
            }
        });

        var result = await controller.Login(
            new LoginRequest { Username = "alice@lab.local", Password = "secret" },
            CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.DoesNotContain("secret", problemDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WithSuccessfulAuthentication_ReturnsApprovedTokenResponse()
    {
        var authService = new FakeAuthenticationService
        {
            Result = new AuthenticationResult
            {
                IsAuthenticated = true,
                FailureCategory = AuthenticationFailureCategory.None
            }
        };
        var mappingService = new FakeAuthorizationMappingService
        {
            Roles = ["Reader"]
        };
        var tokenService = new FakeTokenService();
        var controller = CreateController(authService, mappingService, tokenService);

        var result = await controller.Login(
            new LoginRequest { Username = "alice@lab.local", Password = "secret" },
            CancellationToken.None);

        var objectResult = Assert.IsType<OkObjectResult>(result);
        var tokenResponse = Assert.IsType<TokenResponse>(objectResult.Value);
        Assert.Equal("token-value", tokenResponse.AccessToken);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.True(tokenResponse.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(["Reader"], mappingService.CapturedRoles);
        Assert.Equal("alice@lab.local", tokenService.LastRequest?.Subject);
        Assert.Equal(["Reader"], tokenService.LastRequest?.Roles);
        Assert.Equal(Array.Empty<string>(), tokenService.LastRequest?.Scopes);

        var serialized = JsonSerializer.Serialize(tokenResponse);
        Assert.DoesNotContain("password", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authenticated", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userDistinguishedName", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_UsesAuthenticatedUserGroupsWhenIssuingTokenRoles()
    {
        var authService = new FakeAuthenticationService
        {
            Result = new AuthenticationResult
            {
                IsAuthenticated = true,
                FailureCategory = AuthenticationFailureCategory.None
            }
        };
        var ldapService = new FakeLdapService
        {
            Groups = ["GG-APP-USER", "GG-APP-ADMIN", "GG-UNKNOWN"]
        };
        var mappingService = new FakeAuthorizationMappingService
        {
            Roles = ["Administrator", "Reader"]
        };
        var tokenService = new FakeTokenService();
        var controller = CreateController(authService, ldapService, mappingService, tokenService);

        await controller.Login(
            new LoginRequest { Username = "alice@lab.local", Password = "secret" },
            CancellationToken.None);

        Assert.Equal("alice@lab.local", ldapService.CapturedUserPrincipalName);
        Assert.Equal("secret", ldapService.CapturedPassword);
        Assert.Equal(["GG-APP-USER", "GG-APP-ADMIN", "GG-UNKNOWN"], mappingService.CapturedGroupIdentifiers);
        Assert.Equal(["Administrator"], tokenService.LastRequest?.Roles);
    }

    [Fact]
    public async Task Login_OverHttp_ReturnsBadRequestWithoutCallingAuthenticationService()
    {
        var service = new FakeAuthenticationService();
        var controller = CreateController(service, isHttps: false);

        var result = await controller.Login(
            new LoginRequest { Username = "alice@lab.local", Password = "secret" },
            CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.False(service.WasCalled);
    }

    private static AuthController CreateController(
        FakeAuthenticationService service,
        FakeAuthorizationMappingService? authorizationMappingService = null,
        FakeTokenService? tokenService = null,
        bool isHttps = true)
    {
        return CreateController(
            service,
            new FakeLdapService(),
            authorizationMappingService ?? new FakeAuthorizationMappingService(),
            tokenService ?? new FakeTokenService(),
            isHttps);
    }

    private static AuthController CreateController(
        FakeAuthenticationService service,
        FakeLdapService? ldapService,
        FakeAuthorizationMappingService? authorizationMappingService = null,
        FakeTokenService? tokenService = null,
        bool isHttps = true)
    {
        var controller = new AuthController(
            service,
            ldapService ?? new FakeLdapService(),
            authorizationMappingService ?? new FakeAuthorizationMappingService(),
            tokenService ?? new FakeTokenService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Scheme = isHttps ? "https" : "http";
        return controller;
    }

    private static string GetMessage(AuthenticationFailureCategory category)
    {
        return category switch
        {
            AuthenticationFailureCategory.InvalidCredentials => "Authentication failed.",
            AuthenticationFailureCategory.InvalidRequest => "Invalid request.",
            AuthenticationFailureCategory.DirectoryUnavailable => "Authentication service unavailable.",
            AuthenticationFailureCategory.Timeout => "Authentication request timed out.",
            _ => "Authentication error."
        };
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public AuthenticationResult Result { get; init; } = new()
        {
            IsAuthenticated = false,
            FailureCategory = AuthenticationFailureCategory.InvalidCredentials,
            ErrorMessage = "Authentication failed."
        };

        public bool WasCalled { get; private set; }

        public Task<AuthenticationResult> AuthenticateAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeLdapService : ILdapService
    {
        public string? CapturedUserPrincipalName { get; private set; }
        public string? CapturedPassword { get; private set; }
        public IReadOnlyList<string> Groups { get; set; } = Array.Empty<string>();

        public Task<RootDseResult> QueryRootDseAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> GetUserGroupsAsync(
            string userPrincipalName,
            string password,
            CancellationToken cancellationToken = default)
        {
            CapturedUserPrincipalName = userPrincipalName;
            CapturedPassword = password;
            return Task.FromResult(Groups);
        }
    }

    private sealed class FakeAuthorizationMappingService : IAuthorizationMappingService
    {
        public IReadOnlyList<string> CapturedRoles { get; private set; } = Array.Empty<string>();
        public IReadOnlyCollection<string>? CapturedGroupIdentifiers { get; private set; }
        public IReadOnlyList<string> Roles { get; set; } = ["Reader"];

        public Task<AuthorizationMappingResult> MapGroupsToRolesAsync(
            IReadOnlyCollection<string> groupIdentifiers,
            CancellationToken cancellationToken = default)
        {
            CapturedGroupIdentifiers = groupIdentifiers;
            CapturedRoles = Roles;
            return Task.FromResult(new AuthorizationMappingResult
            {
                Roles = Roles
            });
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public TokenIssuanceRequest? LastRequest { get; private set; }

        public Task<TokenResponse> IssueAsync(
            TokenIssuanceRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new TokenResponse
            {
                AccessToken = "token-value",
                TokenType = "Bearer",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            });
        }
    }
}
