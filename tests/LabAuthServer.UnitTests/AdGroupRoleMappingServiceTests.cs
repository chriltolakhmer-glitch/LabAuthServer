using LabAuthServer.Infrastructure.Identity;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class AdGroupRoleMappingServiceTests
{
    [Theory]
    [InlineData("GG-APP-ADMIN", "Administrator")]
    [InlineData("GG-APP-APPROVER", "Operator")]
    [InlineData("GG-APP-USER", "Reader")]
    [InlineData("GG-APP-REPORT", "Reader")]
    [InlineData("GG-APP-ADMIN,GG-APP-APPROVER", "Administrator,Operator")]
    [InlineData("GG-APP-APPROVER,GG-APP-USER", "Operator,Reader")]
    [InlineData("GG-APP-USER,GG-APP-REPORT", "Reader")]
    [InlineData("GG-APP-ADMIN,GG-APP-USER,GG-APP-REPORT", "Administrator,Reader")]
    [InlineData("GG-UNKNOWN", "")]
    [InlineData("", "")]
    public async Task MapGroupsToRolesAsync_UsesApprovedMappingsAndPrecedence(
        string groups,
        string expectedRoles)
    {
        var service = CreateService(ApprovedMappings());
        var groupIdentifiers = string.IsNullOrEmpty(groups)
            ? Array.Empty<string>()
            : groups.Split(',');

        var result = await service.MapGroupsToRolesAsync(groupIdentifiers);

        Assert.Equal(
            string.IsNullOrEmpty(expectedRoles) ? Array.Empty<string>() : expectedRoles.Split(','),
            result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_IsCaseInsensitiveAndDeduplicatesGroups()
    {
        var service = CreateService(ApprovedMappings());

        var result = await service.MapGroupsToRolesAsync(
            [" gg-app-user ", "GG-APP-USER", "GG-APP-REPORT"]);

        Assert.Equal(["Reader"], result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithMissingMemberOfResultReturnsNoRole()
    {
        var service = CreateService(ApprovedMappings());

        var result = await service.MapGroupsToRolesAsync(Array.Empty<string>());

        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithApprovedMappings_ReturnsMappedRoles()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["GG-APP-USER"] = "Reader",
            ["GG-APP-ADMIN"] = "Administrator"
        });

        var result = await service.MapGroupsToRolesAsync(
            ["GG-APP-USER", "GG-APP-ADMIN"]);

        Assert.Equal(["Administrator", "Reader"], result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithAmbiguousNormalizedMappings_FailsClosed()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            [" GG-APP-USER "] = "Reader",
            ["GG-APP-USER"] = "Reader"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MapGroupsToRolesAsync(["GG-APP-USER"]));

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithUnmappedGroup_ReturnsEmptyRoleSet()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["GG-APP-USER"] = "Reader"
        });

        var result = await service.MapGroupsToRolesAsync(["CN=Unknown,OU=Groups,DC=lab,DC=local"]);

        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithDefaultDenyConfiguration_ReturnsNoRolesWhenNoGroupsMatch()
    {
        var service = CreateService(new Dictionary<string, string>());

        var result = await service.MapGroupsToRolesAsync(["CN=LabReaders,OU=Groups,DC=lab,DC=local"]);

        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithInvalidConfiguration_FailsClosed()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["GG-APP-USER"] = string.Empty
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MapGroupsToRolesAsync(["GG-APP-USER"]));

        Assert.Contains("configuration", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithDeterministicOrdering_ReturnsStableRoleSet()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["GG-APP-USER"] = "Reader",
            ["GG-APP-ADMIN"] = "Administrator"
        });

        var result = await service.MapGroupsToRolesAsync(
            ["GG-APP-USER", "GG-APP-ADMIN"]);

        Assert.Equal(["Administrator", "Reader"], result.Roles);
    }

    [Fact]
    public async Task MapGroupsToRolesAsync_WithEmptyCollection_ReturnsNoRoles()
    {
        var service = CreateService(new Dictionary<string, string>
        {
            ["GG-APP-USER"] = "Reader"
        });

        var result = await service.MapGroupsToRolesAsync(Array.Empty<string>());

        Assert.Empty(result.Roles);
    }

    private static AdGroupRoleMappingService CreateService(Dictionary<string, string> mappings)
    {
        var options = Options.Create(new AuthorizationPolicyOptions
        {
            DefaultDeny = true,
            MaximumGroupCount = 100,
            PublicEndpoints = ["/api/v1/health", "/api/v1/auth/login"],
            GroupToRoleMappings = mappings
        });

        return new AdGroupRoleMappingService(options);
    }

    private static Dictionary<string, string> ApprovedMappings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GG-APP-ADMIN"] = "Administrator",
            ["GG-APP-APPROVER"] = "Operator",
            ["GG-APP-USER"] = "Reader",
            ["GG-APP-REPORT"] = "Reader"
        };
    }
}
