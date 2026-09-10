namespace LabAuthServer.Infrastructure.ActiveDirectory;

public sealed class LdapOptions
{
    public const string SectionName = "ActiveDirectory";

    public string Domain { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 636;

    public string BaseDn { get; set; } = string.Empty;

    public string UserSearchBaseDn { get; set; } = "CN=Users,DC=lab,DC=local";

    public string ServiceAccountUsername { get; set; } = string.Empty;

    public string ServiceAccountPasswordFile { get; set; } = @"C:\ProgramData\LabAuthServer\Secrets\ldap-service-account-password.dpapi";

    public bool UseLdaps { get; set; } = true;

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan AuthenticationTimeout { get; set; } = LabAuthServer.Application.Services.AuthenticationOperation.DefaultTimeout;

    public int MaxConcurrentLdapOperations { get; set; } = 4;

    public int MaxPendingLdapWaiters { get; set; } = 16;

    /// <summary>
    /// Maximum membership values accepted from a single directory result. Bounds
    /// application allocation before the approved authorization group policy applies.
    /// </summary>
    public int MaximumGroupMemberships { get; set; } = 100;
}
