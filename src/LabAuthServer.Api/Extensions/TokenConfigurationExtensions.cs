using LabAuthServer.Application.Constants;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Application.Services;
using LabAuthServer.Infrastructure.Identity;
using LabAuthServer.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Api.Extensions;

public static class TokenConfigurationExtensions
{
    public static IServiceCollection AddTokenConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<TokenOptions>()
            .Bind(configuration.GetSection(TokenOptions.SectionName))
            .Validate(
                options => TokenOptionsValidator.Validate(options).Count == 0,
                "The token configuration is invalid.")
            .ValidateOnStart();

        services
            .AddOptions<AuthorizationPolicyOptions>()
            .Bind(configuration.GetSection(AuthorizationPolicyOptions.SectionName))
            .Validate(
                options => AuthorizationPolicyOptionsValidator.Validate(options).Count == 0,
                "The authorization policy configuration is invalid.")
            .ValidateOnStart();

        services.AddSingleton<IProtectedSigningKeyProvider>(serviceProvider =>
            new CertificateSigningKeyProvider(
                serviceProvider.GetRequiredService<IOptions<TokenOptions>>(),
                serviceProvider.GetRequiredService<ILogger<CertificateSigningKeyProvider>>()));
        services.AddScoped<ITokenSigningService, RsaTokenSigningService>();
        services.AddScoped<ITokenService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TokenOptions>>().Value;
            return new TokenService(
                options.Issuer,
                options.Audience,
                options.AccessTokenLifetime,
                options.MaximumClaimSize,
                options.MaximumTokenSize,
                serviceProvider.GetRequiredService<ITokenSigningService>());
        });
        services.AddScoped<IAuthorizationMappingService, AdGroupRoleMappingService>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerAuthenticationOptions>();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(AuthorizationPolicies.RequireReader, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasClaim("role", AuthorizationRoles.Reader)));

            options.AddPolicy(AuthorizationPolicies.RequireOperator, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasClaim("role", AuthorizationRoles.Operator)));

            options.AddPolicy(AuthorizationPolicies.RequireAdministrator, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasClaim("role", AuthorizationRoles.Administrator)));
        });

        return services;
    }
}
