using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.LicenseIssuer;

/// <summary>
/// Outcome of a license issuance attempt. A structured result is used rather than a boolean so
/// callers can distinguish validation failures from key or serialization failures safely.
/// </summary>
public sealed class LicenseIssuanceResult
{
    private LicenseIssuanceResult(LicenseIssuanceReason reason, SignedLicense? license, byte[]? containerBytes)
    {
        Reason = reason;
        License = license;
        ContainerBytes = containerBytes;
    }

    /// <summary>Internal reason code. Never expose to a customer.</summary>
    public LicenseIssuanceReason Reason { get; }

    /// <summary>The signed license when issuance succeeded; otherwise <c>null</c>.</summary>
    public SignedLicense? License { get; }

    /// <summary>
    /// Serialized license container (payload, algorithm, keyId, signature) when issuance
    /// succeeded; otherwise <c>null</c>. Contains no private key material.
    /// </summary>
    public byte[]? ContainerBytes { get; }

    /// <summary>True only when issuance succeeded.</summary>
    public bool Succeeded => Reason == LicenseIssuanceReason.None && License is not null && ContainerBytes is not null;

    /// <summary>Creates a successful result.</summary>
    public static LicenseIssuanceResult Success(SignedLicense license, byte[] containerBytes)
        => new(
            LicenseIssuanceReason.None,
            license ?? throw new ArgumentNullException(nameof(license)),
            containerBytes ?? throw new ArgumentNullException(nameof(containerBytes)));

    /// <summary>Creates a failed result with an internal reason code and no license output.</summary>
    public static LicenseIssuanceResult Failed(LicenseIssuanceReason reason)
    {
        if (reason == LicenseIssuanceReason.None)
        {
            throw new ArgumentException("A failure result requires a non-None reason.", nameof(reason));
        }

        return new(reason, null, null);
    }
}