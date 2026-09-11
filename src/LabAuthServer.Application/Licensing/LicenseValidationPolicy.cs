namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Static bounds used during validation (Phase 4.5). The exact commercial feature-to-edition
/// mapping remains TO BE CONFIRMED DURING IMPLEMENTATION (Phase 4.6); this type defines only the
/// structural limits needed to reject malformed or out-of-range values.
/// </summary>
public static class LicenseValidationPolicy
{
    /// <summary>Maximum number of feature identifiers accepted in a single license.</summary>
    public const int MaximumFeatureCount = 256;

    /// <summary>Maximum length of a single feature identifier.</summary>
    public const int MaximumFeatureLength = 128;

    /// <summary>Maximum number of limit entries accepted in a single license.</summary>
    public const int MaximumLimitCount = 64;

    /// <summary>Maximum length of a limit key.</summary>
    public const int MaximumLimitKeyLength = 64;

    /// <summary>Largest accepted numeric limit value.</summary>
    public const int MaximumLimitValue = int.MaxValue;

    /// <summary>
    /// Documented clock-skew allowance. The precise operational value remains TO BE CONFIRMED
    /// DURING IMPLEMENTATION (Phase 4.7, O-13); the safest implementation is a small allowance so
    /// ordinary NTP drift does not reject a valid license while expiry is still enforced.
    /// </summary>
    public static readonly TimeSpan ClockSkewAllowance = TimeSpan.FromMinutes(5);
}