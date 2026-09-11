namespace LabAuthServer.Domain.Licensing;

/// <summary>Parsing and ordering helpers for <see cref="LicenseEdition"/>.</summary>
public static class LicenseEditionExtensions
{
    /// <summary>
    /// Parses an edition identifier using ordinal-ignore-case comparison.
    /// Unknown values map to <see cref="LicenseEdition.Unknown"/> rather than throwing.
    /// </summary>
    public static LicenseEdition ParseEdition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LicenseEdition.Unknown;
        }

        return value.Trim() switch
        {
            var v when string.Equals(v, nameof(LicenseEdition.Community), StringComparison.OrdinalIgnoreCase) => LicenseEdition.Community,
            var v when string.Equals(v, nameof(LicenseEdition.Professional), StringComparison.OrdinalIgnoreCase) => LicenseEdition.Professional,
            var v when string.Equals(v, nameof(LicenseEdition.Enterprise), StringComparison.OrdinalIgnoreCase) => LicenseEdition.Enterprise,
            _ => LicenseEdition.Unknown
        };
    }

    /// <summary>True when the edition is one of the approved editions.</summary>
    public static bool IsKnown(this LicenseEdition edition) => edition is not LicenseEdition.Unknown;

    /// <summary>
    /// Rank used by the approved edition hierarchy Community &lt; Professional &lt; Enterprise.
    /// Unknown returns -1 so it never satisfies a minimum-edition check.
    /// </summary>
    public static int Rank(this LicenseEdition edition) => edition switch
    {
        LicenseEdition.Community => 1,
        LicenseEdition.Professional => 2,
        LicenseEdition.Enterprise => 3,
        _ => -1
    };
}