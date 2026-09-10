using System.DirectoryServices.Protocols;

namespace LabAuthServer.Infrastructure.Services;

public sealed record LdapSearchResult(ResultCode ResultCode, IReadOnlyList<LdapSearchEntry> Entries, bool HasReferences = false);
