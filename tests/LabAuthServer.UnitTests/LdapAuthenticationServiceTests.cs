using LabAuthServer.Application.Services;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class LdapAuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_WithValidLabLocalUpn_UsesSuppliedCredentials()
    {
        var client = new FakeLdapAuthenticationClient
        {
            Result = AuthenticationResult.Succeeded("alice@lab.local")
        };
        var service = CreateService(client);

        var result = await service.AuthenticateAsync("alice@lab.local", "secret");

        Assert.True(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.None, result.FailureCategory);
        Assert.Equal("alice@lab.local", client.Username);
        Assert.Equal("secret", client.Password);
        Assert.Equal("alice@lab.local", result.Username);
        Assert.Null(result.DistinguishedName);
    }

    [Fact]
    public async Task AuthenticateAsync_WithDifferentUpnDomain_ReturnsInvalidRequestWithoutBinding()
    {
        var client = new FakeLdapAuthenticationClient();
        var service = CreateService(client);

        var result = await service.AuthenticateAsync("alice@example.com", "secret");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.InvalidRequest, result.FailureCategory);
        Assert.Equal("Authentication failed.", result.ErrorMessage);
        Assert.Null(client.Username);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsGenericFailure()
    {
        var client = new FakeLdapAuthenticationClient
        {
            Result = AuthenticationResult.Failed(new DirectoryFailure(AuthenticationFailureCategory.InvalidCredentials, DirectoryFailureStage.UserBind, DirectoryFailureReason.UnexpectedFailure))
        };
        var service = CreateService(client);

        var result = await service.AuthenticateAsync("alice@lab.local", "wrong");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.InvalidCredentials, result.FailureCategory);
        Assert.Equal("Authentication failed.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(AuthenticationFailureCategory.DirectoryUnavailable, AuthenticationFailureCategory.DirectoryUnavailable, "Authentication service unavailable.")]
    [InlineData(AuthenticationFailureCategory.Timeout, AuthenticationFailureCategory.Timeout, "Authentication request timed out.")]
    [InlineData(AuthenticationFailureCategory.Cancelled, AuthenticationFailureCategory.Cancelled, "Authentication request was cancelled.")]
    [InlineData(AuthenticationFailureCategory.Unexpected, AuthenticationFailureCategory.Unexpected, "Authentication error.")]
    public async Task AuthenticateAsync_MapsInfrastructureFailures(
        AuthenticationFailureCategory bindFailure,
        AuthenticationFailureCategory expectedFailure,
        string expectedMessage)
    {
        var client = new FakeLdapAuthenticationClient
        {
            Result = AuthenticationResult.Failed(new DirectoryFailure(bindFailure, DirectoryFailureStage.UserBind, DirectoryFailureReason.UnexpectedFailure))
        };
        var service = CreateService(client);

        var result = await service.AuthenticateAsync("alice@lab.local", "secret");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(expectedFailure, result.FailureCategory);
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCancelledBeforeBinding_DoesNotCallClient()
    {
        var client = new FakeLdapAuthenticationClient();
        var service = CreateService(client);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var result = await service.AuthenticateAsync("alice@lab.local", "secret", cancellationTokenSource.Token);

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.Cancelled, result.FailureCategory);
        Assert.Null(client.Username);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialProviderFails_ReturnsConfigurationFailure()
    {
        var service = CreateService(
            new FakeLdapAuthenticationClient(),
            credentialProvider: new FailingCredentialProvider());

        var result = await service.AuthenticateAsync("alice@lab.local", "secret");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.Configuration, result.FailureCategory);
        Assert.Equal("Authentication service unavailable.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(false, 389)]
    [InlineData(true, 389)]
    [InlineData(false, 636)]
    public async Task AuthenticateAsync_WhenLdapsConfigurationIsInvalid_DoesNotCallClient(
        bool useLdaps,
        int port)
    {
        var client = new FakeLdapAuthenticationClient();
        var service = CreateService(client, useLdaps, port);

        var result = await service.AuthenticateAsync("alice@lab.local", "secret");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.Configuration, result.FailureCategory);
        Assert.Null(client.Username);
    }

    private static LdapAuthenticationService CreateService(
        FakeLdapAuthenticationClient client,
        bool useLdaps = true,
        int port = 636,
        ILdapServiceAccountCredentialProvider? credentialProvider = null)
    {
        var options = Options.Create(new LdapOptions
        {
            Domain = "lab.local",
            Host = "DC01.lab.local",
            Port = port,
            BaseDn = "DC=lab,DC=local",
            UseLdaps = useLdaps,
            ConnectionTimeout = TimeSpan.FromSeconds(10)
        });

        return new LdapAuthenticationService(
            options,
            client,
            credentialProvider ?? new NullCredentialProvider(),
            NullLogger<LdapAuthenticationService>.Instance);
    }

    private sealed class NullCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("test-service-credential");
    }

    private sealed class FailingCredentialProvider : ILdapServiceAccountCredentialProvider
    {
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("LDAP service-account password is not configured.");
    }

    private sealed class FakeLdapAuthenticationClient : ILdapAuthenticationClient
    {
        public AuthenticationResult Result { get; init; } = AuthenticationResult.Failed(new DirectoryFailure(AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureStage.ConnectionSetup, DirectoryFailureReason.TransportFailure));

        public string? Username { get; private set; }

        public string? Password { get; private set; }

        public async Task<AuthenticationResult> AuthenticateAsync(
            string username,
            string password,
            LdapOptions options,
            ILdapServiceAccountCredentialProvider credentialProvider,
            CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            await credentialProvider.GetPasswordAsync(cancellationToken);
            Username = username;
            Password = password;
            return Result.IsAuthenticated ? AuthenticationResult.Succeeded(username) : AuthenticationResult.Failed(Result.Failure!);
        }

        public Task<AuthenticationResult> BindAsync(
            string username,
            string password,
            LdapOptions options,
            CancellationToken cancellationToken = default, AuthenticationOperation? operation = null)
        {
            Username = username;
            Password = password;
            return Task.FromResult(Result);
        }
    }
}
