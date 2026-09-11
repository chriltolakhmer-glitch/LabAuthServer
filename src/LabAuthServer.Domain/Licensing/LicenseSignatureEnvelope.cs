namespace LabAuthServer.Domain.Licensing;

/// <summary>
/// Signature envelope carried alongside the signed payload.
/// The final on-disk envelope shape remains TO BE CONFIRMED DURING IMPLEMENTATION;
/// this type defines only the fields the verifier needs (algorithm, keyId, signature).
/// </summary>
public sealed record LicenseSignatureEnvelope
{
    /// <summary>Signing algorithm identifier, for example <see cref="LicenseConstants.RsaPssSha256Algorithm"/>.</summary>
    public required string Algorithm { get; init; }

    /// <summary>Identifier of the signing key, used to select the trusted verification key.</summary>
    public required string KeyId { get; init; }

    /// <summary>Base64 of the raw signature bytes.</summary>
    public required string Signature { get; init; }
}