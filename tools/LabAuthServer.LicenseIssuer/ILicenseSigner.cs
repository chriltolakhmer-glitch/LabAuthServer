using System.Security.Cryptography;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Signs exact license payload bytes. Separated from issuer orchestration so the low-level
/// cryptographic operation stays behind a testable boundary and never couples to request handling.
/// </summary>
public interface ILicenseSigner
{
    /// <summary>
    /// Returns the raw signature over <paramref name="payload"/> produced with <paramref name="key"/>.
    /// Implementations must not log the payload or the signature.
    /// </summary>
    byte[] Sign(byte[] payload, RSA key);
}