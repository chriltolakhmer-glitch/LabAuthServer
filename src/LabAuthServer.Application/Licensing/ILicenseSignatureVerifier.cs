namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Cryptographic verification boundary. Implementations verify a signature over the exact
/// signed payload bytes using a trusted public key selected by key identifier.
/// No private key is ever required by this abstraction (Phase 4 decisions D-11, D-16).
/// </summary>
public interface ILicenseSignatureVerifier
{
    /// <summary>
    /// Verifies <paramref name="signature"/> over <paramref name="signedPayload"/> using the
    /// trusted public key identified by <paramref name="keyId"/> and
    /// <paramref name="algorithm"/>.
    /// </summary>
    /// <returns>
    /// A structured outcome. Unknown keys, unsupported algorithms and cryptographic
    /// failures are reported as failures, never as success.
    /// </returns>
    LicenseSignatureVerificationOutcome Verify(
        byte[] signedPayload,
        string algorithm,
        string keyId,
        byte[] signature);
}

/// <summary>Result of a signature verification attempt.</summary>
public sealed class LicenseSignatureVerificationOutcome
{
    private LicenseSignatureVerificationOutcome(bool succeeded, LicenseValidationReason? failure)
    {
        Succeeded = succeeded;
        Failure = failure;
    }

    /// <summary>True only when the signature verified.</summary>
    public bool Succeeded { get; }

    /// <summary>The structured failure reason when verification failed; otherwise <c>null</c>.</summary>
    public LicenseValidationReason? Failure { get; }

    /// <summary>Creates a successful outcome.</summary>
    public static LicenseSignatureVerificationOutcome Success() => new(true, null);

    /// <summary>Creates a failed outcome with an internal reason code.</summary>
    public static LicenseSignatureVerificationOutcome Failed(LicenseValidationReason reason)
        => new(false, reason);
}