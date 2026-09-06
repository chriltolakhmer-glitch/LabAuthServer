using LabAuthServer.Application.Constants;

namespace LabAuthServer.Infrastructure.Security;

public static class AuthorizationPolicyOptionsValidator
{
    public static IReadOnlyList<string> Validate(AuthorizationPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.DefaultDeny is not true)
        {
            failures.Add("Authorization default deny must be explicitly enabled.");
        }

        if (options.MaximumGroupCount <= 0 || options.MaximumGroupCount > 1000)
        {
            failures.Add("Authorization maximum group count must be between 1 and 1000.");
        }

        if (options.PublicEndpoints.Any(endpoint => string.IsNullOrWhiteSpace(endpoint) || !endpoint.StartsWith('/')))
        {
            failures.Add("Authorization public endpoints must be non-empty API paths.");
        }

        if (options.PublicEndpoints.Count != options.PublicEndpoints.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            failures.Add("Authorization public endpoints must be unique.");
        }

        var seenGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var approvedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GG-APP-ADMIN",
            "GG-APP-APPROVER",
            "GG-APP-USER",
            "GG-APP-REPORT"
        };
        var approvedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AuthorizationRoles.Administrator,
            AuthorizationRoles.Operator,
            AuthorizationRoles.Reader
        };

        foreach (var mapping in options.GroupToRoleMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key) || string.IsNullOrWhiteSpace(mapping.Value))
            {
                failures.Add("Authorization group-to-role mappings must have non-empty group and role values.");
                break;
            }

            if (mapping.Key.Length > 512 || mapping.Value.Length > 256)
            {
                failures.Add("Authorization group-to-role mapping values exceed their maximum length.");
                break;
            }

            var normalizedKey = mapping.Key.Trim();
            if (!seenGroupKeys.Add(normalizedKey))
            {
                failures.Add("Authorization group-to-role mappings must be unique and unambiguous.");
                break;
            }

            if (!approvedGroups.Contains(normalizedKey) || !approvedRoles.Contains(mapping.Value.Trim()))
            {
                failures.Add("Authorization group-to-role mappings contain an unapproved group or role.");
                break;
            }
        }

        return failures;
    }
}
