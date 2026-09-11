using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Parses raw license bytes into a strongly typed document plus the exact signed payload.
/// Implementations must reject ambiguous input (duplicate keys, trailing data, wrong casing).
/// </summary>
public interface ILicenseDocumentParser
{
    /// <summary>
    /// Attempts to parse <paramref name="content"/>.
    /// Returns a typed failure rather than throwing for malformed input.
    /// </summary>
    LicenseParseOutcome Parse(ReadOnlySpan<byte> content);
}

/// <summary>Result of a parse attempt: either a parsed document or a structured failure.</summary>
public sealed class LicenseParseOutcome
{
    private LicenseParseOutcome(SignedLicense? license, LicenseValidationReason? failure)
    {
        License = license;
        Failure = failure;
    }

    /// <summary>The parsed license when parsing succeeded; otherwise <c>null</c>.</summary>
    public SignedLicense? License { get; }

    /// <summary>The structured failure reason when parsing failed; otherwise <c>null</c>.</summary>
    public LicenseValidationReason? Failure { get; }

    /// <summary>True when parsing succeeded.</summary>
    public bool Succeeded => License is not null;

    /// <summary>Creates a successful outcome.</summary>
    public static LicenseParseOutcome Success(SignedLicense license)
        => new(license ?? throw new ArgumentNullException(nameof(license)), null);

    /// <summary>Creates a failed outcome with an internal reason code.</summary>
    public static LicenseParseOutcome Failed(LicenseValidationReason reason) => new(null, reason);
}