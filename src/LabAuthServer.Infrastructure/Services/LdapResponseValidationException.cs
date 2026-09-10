namespace LabAuthServer.Infrastructure.Services;

internal sealed class LdapResponseValidationException : Exception
{
    public LdapResponseValidationException() : base("The directory response could not be validated.") { }
}
