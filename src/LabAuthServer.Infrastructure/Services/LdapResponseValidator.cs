using System.DirectoryServices.Protocols;
using System.Globalization;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;

namespace LabAuthServer.Infrastructure.Services;

internal static class LdapResponseValidator
{
    public static DirectoryFailure? ValidateSearch(LdapSearchResult? response, DirectoryFailureStage operationStage)
    {
        if (response is null)
            return Invalid(DirectoryFailureReason.InvalidResponse);
        if (response.ResultCode != ResultCode.Success)
            return LdapFailureClassifier.FromCode((int)response.ResultCode, DirectoryDiagnosticSource.OperationResult, operationStage);
        if (response.HasReferences)
            return Invalid(DirectoryFailureReason.InvalidResponse);
        if (response.Entries.Count != 1)
            return Invalid(DirectoryFailureReason.UnexpectedEntryCount);
        return null;
    }

    public static bool TryIdentity(LdapSearchEntry entry, string username, out int accountControl)
    {
        accountControl = 0;
        var upn = SingleAttribute(entry, "userPrincipalName");
        var dn = SingleAttribute(entry, "distinguishedName");
        return LdapDistinguishedNameParser.TryParse(entry.DistinguishedName, out _)
            && string.Equals(dn, entry.DistinguishedName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(upn, username, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(SingleAttribute(entry, "userAccountControl"), NumberStyles.None, CultureInfo.InvariantCulture, out accountControl)
            && accountControl >= 0;
    }

    public static string? SingleAttribute(LdapSearchEntry entry, string name)
        => entry.Attributes.TryGetValue(name, out var values) && values.Count == 1 ? values[0] : null;

    public static DirectoryFailure Invalid(DirectoryFailureReason reason)
        => new(AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureStage.ResponseValidation, reason);
}
