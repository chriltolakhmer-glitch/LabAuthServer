namespace LabAuthServer.Infrastructure.Services;

public sealed record LdapSearchEntry(string DistinguishedName, IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes);
