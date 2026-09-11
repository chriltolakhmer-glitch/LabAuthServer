namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// Known feature identifiers the application can gate (Phase 4.6, decision D4.6-1).
/// The set is the implementation catalog and the approved feature-to-edition matrix (O-01).
/// An identifier absent from this set is default-deny and must never be treated as granted.
/// </summary>
public static class LicenseFeatureIds
{
    /// <summary>Basic (non-directory) authentication capability.</summary>
    public const string AuthBasic = "auth.basic";

    /// <summary>JWT issuance capability.</summary>
    public const string AuthJwt = "auth.jwt";

    /// <summary>LDAP/LDAPS directory authentication capability.</summary>
    public const string AuthLdap = "auth.ldap";

    /// <summary>Persisted audit logging capability.</summary>
    public const string AuditLogging = "audit.logging";

    /// <summary>Administrative console capability.</summary>
    public const string AdminConsole = "admin.console";

    private static readonly string[] Known =
    {
        AuthBasic,
        AuthJwt,
        AuthLdap,
        AuditLogging,
        AdminConsole
    };

    private static readonly IReadOnlyDictionary<string, LicenseEdition> MinimumEditions =
        new Dictionary<string, LicenseEdition>(StringComparer.Ordinal)
        {
            [AuthBasic] = LicenseEdition.Community,
            [AuthJwt] = LicenseEdition.Community,
            [AuthLdap] = LicenseEdition.Professional,
            [AuditLogging] = LicenseEdition.Professional,
            [AdminConsole] = LicenseEdition.Enterprise
        };

    /// <summary>All known feature identifiers.</summary>
    public static IReadOnlyList<string> All => Known;

    /// <summary>True when <paramref name="featureId"/> is a known feature identifier.</summary>
    public static bool IsKnown(string? featureId)
        => featureId is not null && Array.IndexOf(Known, featureId) >= 0;

    /// <summary>
    /// True when the approved edition matrix permits the feature. This is an upper bound,
    /// not an entitlement: the signed license must still explicitly list the feature.
    /// </summary>
    public static bool IsAllowedForEdition(string? featureId, LicenseEdition edition)
        => featureId is not null
            && edition.IsKnown()
            && MinimumEditions.TryGetValue(featureId, out var minimumEdition)
            && edition.Rank() >= minimumEdition.Rank();

    /// <summary>
    /// True when the identifier has the documented shape: lowercase ASCII, optionally
    /// dot/dash/underscore separated, starting with a lowercase letter. Structural check only.
    /// </summary>
    public static bool IsWellFormed(string? featureId)
    {
        if (string.IsNullOrEmpty(featureId) || !char.IsAsciiLetterLower(featureId[0]))
        {
            return false;
        }

        foreach (var character in featureId)
        {
            var allowed = char.IsAsciiLetterLower(character)
                || char.IsAsciiDigit(character)
                || character is '.' or '-' or '_';

            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }
}