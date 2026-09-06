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
            Result = new LdapBindResult { IsSuccess = true }
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
            Result = new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = LdapBindFailureCategory.InvalidCredentials
            }
        };
        var service = CreateService(client);

        var result = await service.AuthenticateAsync("alice@lab.local", "wrong");

        Assert.False(result.IsAuthenticated);
        Assert.Equal(AuthenticationFailureCategory.InvalidCredentials, result.FailureCategory);
        Assert.Equal("Authentication failed.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(LdapBindFailureCategory.DirectoryUnavailable, AuthenticationFailureCategory.DirectoryUnavailable, "Authentication service unavailable.")]
    [InlineData(LdapBindFailureCategory.Timeout, AuthenticationFailureCategory.Timeout, "Authentication request timed out.")]
    [InlineData(LdapBindFailureCategory.Cancelled, AuthenticationFailureCategory.Cancelled, "Authentication request was cancelled.")]
    [InlineData(LdapBindFailureCategory.Unexpected, AuthenticationFailureCategory.Unexpected, "Authentication error.")]
    public async Task AuthenticateAsync_MapsInfrastructureFailures(
        LdapBindFailureCategory bindFailure,
        AuthenticationFailureCategory expectedFailure,
        string expectedMessage)
    {
        var client = new FakeLdapAuthenticationClient
        {
            Result = new LdapBindResult
            {
                IsSuccess = false,
                FailureCategory = bindFailure
            }
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
        public LdapBindResult Result { get; init; } = new()
        {
            IsSuccess = false,
            FailureCategory = LdapBindFailureCategory.DirectoryUnavailable
        };

        public string? Username { get; private set; }

        public string? Password { get; private set; }

        public async Task<LdapAuthenticationResult> AuthenticateAsync(
            string username,
            string password,
            LdapOptions options,
            ILdapServiceAccountCredentialProvider credentialProvider,
            CancellationToken cancellationToken = default)
        {
            await credentialProvider.GetPasswordAsync(cancellationToken);
            Username = username;
            Password = password;
            return new LdapAuthenticationResult
            {
                IsSuccess = Result.IsSuccess,
                FailureCategory = Result.FailureCategory,
                Username = username
            };
        }

        public Task<LdapBindResult> BindAsync(
            string username,
            string password,
            LdapOptions options,
            CancellationToken cancellationToken = default)
        {
            Username = username;
            Password = password;
            return Task.FromResult(Result);
        }
    }
}
