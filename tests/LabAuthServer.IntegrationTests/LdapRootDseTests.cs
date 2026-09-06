using LabAuthServer.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LabAuthServer.IntegrationTests;

public sealed class LdapRootDseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LdapRootDseTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    [Fact]
    public async Task QueryRootDseAsync_ReturnsResultWithStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scope = _factory.Services.CreateScope();
        var ldapService = scope.ServiceProvider.GetRequiredService<ILdapService>();

        // Act
        var result = await ldapService.QueryRootDseAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess || result.ErrorMessage != null, "Result should either succeed or have an error message");
    }

    [Fact]
    public async Task QueryRootDseAsync_WhenSuccessful_ReturnsAttributes()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var ldapService = scope.ServiceProvider.GetRequiredService<ILdapService>();

        // Act
        var result = await ldapService.QueryRootDseAsync();

        // Assert
        if (result.IsSuccess)
        {
            Assert.NotNull(result.Attributes);
            Assert.NotEmpty(result.Attributes);
        }
        else
        {
            // If connectivity fails (e.g., DC not available), we still have a result
            Assert.Null(result.Attributes);
            Assert.NotNull(result.ErrorMessage);
        }
    }

    [Fact]
    public async Task QueryRootDseAsync_ReturnsNullAttributesOnFailure()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var ldapService = scope.ServiceProvider.GetRequiredService<ILdapService>();

        // Act
        var result = await ldapService.QueryRootDseAsync();

        // Assert
        if (!result.IsSuccess)
        {
            Assert.Null(result.Attributes);
        }
    }

    [Fact]
    public async Task QueryRootDseAsync_RespectsCancellation()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var ldapService = scope.ServiceProvider.GetRequiredService<ILdapService>();
        var cts = new CancellationTokenSource(TimeSpan.Zero);

        // Act
        var result = await ldapService.QueryRootDseAsync(cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}
