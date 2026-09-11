namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// Known limit identifiers a license may define (Phase 4.6, decision D-06).
/// A limit absent from a license is never treated as unlimited.
/// </summary>
public static class LicenseLimitKeys
{
    /// <summary>Maximum number of licensed users.</summary>
    public const string MaximumUsers = "max.users";

    private static readonly string[] Known =
    {
        MaximumUsers
    };

    /// <summary>All known limit identifiers.</summary>
    public static IReadOnlyList<string> All => Known;

    /// <summary>True when <paramref name="limitKey"/> is a known limit identifier.</summary>
    public static bool IsKnown(string? limitKey)
        => limitKey is not null && Array.IndexOf(Known, limitKey) >= 0;
}