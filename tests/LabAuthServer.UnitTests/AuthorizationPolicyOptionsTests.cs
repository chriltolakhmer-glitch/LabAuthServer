using LabAuthServer.Application.Constants;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LabAuthServer.UnitTests;

public sealed class AuthorizationPolicyOptionsTests
{
    [Fact]
    public void ApprovedRoles_And_Policies_AreRegistered()
    {
        Assert.Equal("Reader", AuthorizationRoles.Reader);
        Assert.Equal("Operator", AuthorizationRoles.Operator);
        Assert.Equal("Administrator", AuthorizationRoles.Administrator);

        Assert.Equal("RequireReader", AuthorizationPolicies.RequireReader);
        Assert.Equal("RequireOperator", AuthorizationPolicies.RequireOperator);
        Assert.Equal("RequireAdministrator", AuthorizationPolicies.RequireAdministrator);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(AuthorizationPolicies.RequireReader, policy =>
                policy.RequireAuthenticatedUser().RequireAssertion(context =>
                    context.User.HasClaim("role", AuthorizationRoles.Reader)));

            options.AddPolicy(AuthorizationPolicies.RequireOperator, policy =>
                policy.RequireAuthenticatedUser().RequireAssertion(context =>
                    context.User.HasClaim("role", AuthorizationRoles.Operator)));

            options.AddPolicy(AuthorizationPolicies.RequireAdministrator, policy =>
                policy.RequireAuthenticatedUser().RequireAssertion(context =>
                    context.User.HasClaim("role", AuthorizationRoles.Administrator)));
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.DefaultPolicy);
        Assert.NotNull(options.GetPolicy(AuthorizationPolicies.RequireReader));
        Assert.NotNull(options.GetPolicy(AuthorizationPolicies.RequireOperator));
        Assert.NotNull(options.GetPolicy(AuthorizationPolicies.RequireAdministrator));
    }

    [Fact]
    public void Validate_WithExplicitDefaultDenyAndAllowlist_ReturnsNoFailures()
    {
        var options = new AuthorizationPolicyOptions
        {
            DefaultDeny = true,
            MaximumGroupCount = 100,
            PublicEndpoints = ["/api/v1/health", "/api/v1/auth/login"],
            GroupToRoleMappings = new Dictionary<string, string>
            {
                ["GG-APP-USER"] = "Reader"
            }
        };

        var failures = AuthorizationPolicyOptionsValidator.Validate(options);

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_WithoutDefaultDeny_ReturnsFailure()
    {
        var failures = AuthorizationPolicyOptionsValidator.Validate(new AuthorizationPolicyOptions
        {
            DefaultDeny = false,
            MaximumGroupCount = 1
        });

        Assert.Contains(failures, failure => failure.Contains("default deny", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithInvalidEndpointOrMapping_ReturnsFailures()
    {
        var failures = AuthorizationPolicyOptionsValidator.Validate(new AuthorizationPolicyOptions
        {
            DefaultDeny = true,
            MaximumGroupCount = 0,
            PublicEndpoints = ["health"],
            GroupToRoleMappings = new Dictionary<string, string>
            {
                [string.Empty] = "Reader"
            }
        });

        Assert.Contains(failures, failure => failure.Contains("group count", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failures, failure => failure.Contains("mappings", StringComparison.OrdinalIgnoreCase));
    }
}
