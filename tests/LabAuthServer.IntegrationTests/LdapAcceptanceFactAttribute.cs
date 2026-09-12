namespace LabAuthServer.IntegrationTests;

public sealed class LdapAcceptanceFactAttribute : FactAttribute
{
    public const string EnableVariable = "LABAUTHSERVER_RUN_LDAP_ACCEPTANCE";
    public LdapAcceptanceFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnableVariable), "1", StringComparison.Ordinal))
            Skip = $"Real LDAPS acceptance is opt-in: set {EnableVariable}=1 and all LABAUTHSERVER_LDAP_TEST_* target values documented in docs/Testing.md.";
    }
}
