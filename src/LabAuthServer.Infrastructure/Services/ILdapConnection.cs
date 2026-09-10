using System.DirectoryServices.Protocols;
using System.Net;

namespace LabAuthServer.Infrastructure.Services;

/// <summary>Narrow synchronous seam over the existing native LDAP provider.</summary>
public interface ILdapConnection : IDisposable
{
    void ConfigureSession(bool useLdaps);
    void Bind(NetworkCredential credential);
    LdapSearchResult? Search(SearchRequest request);
}
