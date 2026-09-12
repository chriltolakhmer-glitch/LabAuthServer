using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.ActiveDirectory;
using LabAuthServer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapRootDseTests
{
    [Fact]
    public async Task QueryRootDse_WithoutHostCredentials_ReturnsSafeFailure()
    {
        using var factory = new InfrastructureSafeApiFactory();
        using var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ILdapService>().QueryRootDseAsync();
        Assert.False(result.IsSuccess);
        Assert.Null(result.Attributes);
        Assert.Equal("The directory service could not complete the Root DSE query.", result.ErrorMessage);
    }

    [Fact]
    public async Task QueryRootDse_WithoutServiceUsername_StopsBeforeCredentials()
    {
        var credentials = new ForbiddenCredentials();
        var service = new LdapService(Options.Create(new LdapOptions { ServiceAccountUsername = string.Empty }),
            credentials, NullLogger<LdapService>.Instance, new LdapConnectionFactory());
        var result = await service.QueryRootDseAsync();
        Assert.False(result.IsSuccess);
        Assert.Null(result.Attributes);
        Assert.Equal("The directory service could not complete the Root DSE query.", result.ErrorMessage);
        Assert.Equal(0, credentials.Calls);
    }

    [Fact]
    public async Task QueryRootDse_WhenAlreadyCancelled_ReturnsCancellationWithoutHostCredentials()
    {
        using var factory = new InfrastructureSafeApiFactory();
        using var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ILdapService>()
            .QueryRootDseAsync(new CancellationToken(canceled: true));
        Assert.False(result.IsSuccess);
        Assert.Null(result.Attributes);
        Assert.Equal("The operation was cancelled.", result.ErrorMessage);
    }

    private sealed class ForbiddenCredentials : ILdapServiceAccountCredentialProvider
    {
        public int Calls { get; private set; }
        public Task<string> GetPasswordAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Missing username must stop before credential access.");
        }
    }
}
