namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Supplies the effective licensing policy to licensed feature code (Phase 4.6, decision D4.6-5).
/// The concrete source of a validated license is implementation-specific; implementations
/// must re-evaluate time-sensitive validity during policy access. An implementation must fail closed: when no valid license is available it returns
/// <see cref="LicensePolicy.Restricted"/>.
/// </summary>
public interface ILicensePolicyProvider
{
    /// <summary>Returns the effective policy. Never returns <c>null</c>.</summary>
    ILicensePolicy GetPolicy();
}