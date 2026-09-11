namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Validates raw license container bytes into a typed <see cref="LicenseValidationResult"/>.
/// Validation is a pure function of the supplied bytes, the configured trusted key set and the
/// injected clock (Phase 4.5 decisions D4.5-2, D4.5-3). Every failure path fails closed and no
/// implementation may require a private key.
/// </summary>
public interface ILicenseValidator
{
    /// <summary>
    /// Validates <paramref name="content"/> in the documented order and returns a structured
    /// result. Malformed input yields a deny result rather than an exception.
    /// </summary>
    LicenseValidationResult Validate(ReadOnlySpan<byte> content);
}