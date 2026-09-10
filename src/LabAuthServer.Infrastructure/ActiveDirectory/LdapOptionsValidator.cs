namespace LabAuthServer.Infrastructure.ActiveDirectory;

public static class LdapOptionsValidator
{
    public static IReadOnlyList<string> Validate(LdapOptions options, bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Domain))
        {
            failures.Add("Active Directory domain is required.");
        }

        ValidateHost(options.Host, failures);

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Active Directory port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseDn))
        {
            failures.Add("Active Directory base DN is required.");
        }
        else if (!IsValidDistinguishedName(options.BaseDn))
        {
            failures.Add("Active Directory base DN is invalid.");
        }

        if (string.IsNullOrWhiteSpace(options.UserSearchBaseDn))
        {
            failures.Add("Active Directory user search base DN is required.");
        }
        else if (!IsValidDistinguishedName(options.UserSearchBaseDn))
        {
            failures.Add("Active Directory user search base DN is invalid.");
        }

        if (string.IsNullOrWhiteSpace(options.ServiceAccountUsername))
        {
            failures.Add("Active Directory service-account username is required.");
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceAccountPasswordFile) &&
            !Path.IsPathRooted(options.ServiceAccountPasswordFile))
        {
            failures.Add("Active Directory service-account secret file path must be absolute.");
        }

        if (options.ConnectionTimeout <= TimeSpan.Zero)
        {
            failures.Add("Active Directory connection timeout must be greater than zero.");
        }

        if (!LabAuthServer.Application.Services.AuthenticationOperation.IsValidTimeout(options.AuthenticationTimeout))
        {
            failures.Add("Active Directory authentication timeout must be between 1 and 60 seconds.");
        }

        if (options.MaxConcurrentLdapOperations is < 1 or > 32)
        {
            failures.Add("Active Directory maximum concurrent LDAP operations must be between 1 and 32.");
        }

        if (options.MaxPendingLdapWaiters is < 1 or > 128)
        {
            failures.Add("Active Directory maximum pending LDAP waiters must be between 1 and 128.");
        }

        if (options.MaximumGroupMemberships is < 1 or > 1000)
        {
            failures.Add("Active Directory maximum group memberships must be between 1 and 1000.");
        }

        if (options.UseLdaps && options.Port != 636)
        {
            failures.Add("Active Directory port must be 636 when LDAPS is enabled.");
        }

        if (isProduction && !options.UseLdaps)
        {
            failures.Add("LDAPS must be enabled in Production.");
        }

        return failures;
    }

    private static void ValidateHost(string host, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            failures.Add("Active Directory host is required.");
            return;
        }

        if (host.Contains("ldap://", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("ldaps://", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Active Directory host must not include an LDAP scheme.");
            return;
        }

        if (host.Contains(':'))
        {
            failures.Add("Active Directory host must not include an embedded port.");
            return;
        }

        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            failures.Add("Active Directory host must be a valid DNS name or IP address.");
        }
    }

    private static bool IsValidDistinguishedName(string distinguishedName)
    {
        var componentStart = 0;
        var isEscaped = false;

        for (var index = 0; index < distinguishedName.Length; index++)
        {
            var character = distinguishedName[index];

            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (character == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (character == ',')
            {
                if (!IsValidRelativeDistinguishedName(distinguishedName[componentStart..index]))
                {
                    return false;
                }

                componentStart = index + 1;
            }
        }

        return !isEscaped && IsValidRelativeDistinguishedName(distinguishedName[componentStart..]);
    }

    private static bool IsValidRelativeDistinguishedName(string relativeDistinguishedName)
    {
        var isEscaped = false;

        for (var index = 0; index < relativeDistinguishedName.Length; index++)
        {
            var character = relativeDistinguishedName[index];

            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (character == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (character == '=')
            {
                var attributeName = relativeDistinguishedName[..index].Trim();
                var attributeValue = relativeDistinguishedName[(index + 1)..].Trim();

                return attributeName.Length > 0 && attributeValue.Length > 0;
            }
        }

        return false;
    }
}
