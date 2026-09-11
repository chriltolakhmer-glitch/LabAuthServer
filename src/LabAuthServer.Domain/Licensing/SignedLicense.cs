namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// A parsed license together with the exact signed payload bytes and its signature envelope.
/// Cryptographic verification is performed over <see cref="SignedPayload"/> verbatim,
/// so no canonicalization decision is implied by this model.
/// </summary>
public sealed record SignedLicense
{
    /// <summary>Strongly typed view of the signed payload.</summary>
    public required LicenseDocument Document { get; init; }

    /// <summary>Exact bytes covered by the signature.</summary>
    public required byte[] SignedPayload { get; init; }

    /// <summary>Signature envelope describing the algorithm, signing key and signature bytes.</summary>
    public required LicenseSignatureEnvelope Signature { get; init; }
}