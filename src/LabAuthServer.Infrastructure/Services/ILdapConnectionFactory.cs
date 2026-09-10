using LabAuthServer.Infrastructure.ActiveDirectory;

namespace LabAuthServer.Infrastructure.Services;

public interface ILdapConnectionFactory
{
    ILdapConnection Create(LdapOptions options);
}
