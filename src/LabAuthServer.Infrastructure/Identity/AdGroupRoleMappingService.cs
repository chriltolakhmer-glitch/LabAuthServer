using LabAuthServer.Application.Constants;
using LabAuthServer.Application.Interfaces;
using LabAuthServer.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Identity;

public sealed class AdGroupRoleMappingService : IAuthorizationMappingService
{
    private static readonly string[] RolePrecedence =
    [
        AuthorizationRoles.Administrator,
        AuthorizationRoles.Operator,
        AuthorizationRoles.Reader
    ];

    private readonly IOptions<AuthorizationPolicyOptions> _authorizationOptions;

    public AdGroupRoleMappingService(IOptions<AuthorizationPolicyOptions> authorizationOptions)
    {
        _authorizationOptions = authorizationOptions ?? throw new ArgumentNullException(nameof(authorizationOptions));
    }

    public Task<AuthorizationMappingResult> MapGroupsToRolesAsync(
        IReadOnlyCollection<string> groupIdentifiers,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(groupIdentifiers);

        var options = _authorizationOptions.Value;
        var failures = AuthorizationPolicyOptionsValidator.Validate(options);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Authorization policy configuration is invalid.");
        }

        if (groupIdentifiers.Count == 0)
        {
            return Task.FromResult(new AuthorizationMappingResult { Roles = Array.Empty<string>() });
        }

        if (groupIdentifiers.Count > options.MaximumGroupCount)
        {
            throw new InvalidOperationException("The requested group set exceeds the approved maximum group count.");
        }

        var approvedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupIdentifier in groupIdentifiers.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(groupIdentifier))
            {
                throw new InvalidOperationException("Group identifiers must be non-empty.");
            }

            var trimmedGroup = groupIdentifier.Trim();

            if (!options.GroupToRoleMappings.TryGetValue(trimmedGroup, out var roleName) ||
                string.IsNullOrWhiteSpace(roleName))
            {
                continue;
            }

            approvedRoles.Add(roleName.Trim());
        }

        var orderedRoles = RolePrecedence
            .Where(approvedRoles.Contains)
            .ToArray();

        return Task.FromResult(new AuthorizationMappingResult
        {
            Roles = orderedRoles
        });
    }
}
