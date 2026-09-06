namespace LabAuthServer.Infrastructure.Security;

public sealed class TokenOptions
{
    public const string SectionName = "Token";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; }

    public TimeSpan ClockSkew { get; set; }

    public string SigningAlgorithm { get; set; } = string.Empty;

    public string ActiveKeyId { get; set; } = string.Empty;

    public string PreviousKeyId { get; set; } = string.Empty;

    public DateTimeOffset? PreviousKeyExpiresAt { get; set; }

    public IReadOnlyList<string> ApprovedKeyIds { get; set; } = Array.Empty<string>();

    public string SigningCertificateStoreLocation { get; set; } = string.Empty;

    public string SigningCertificateStoreName { get; set; } = string.Empty;

    public string SigningCertificateThumbprint { get; set; } = string.Empty;

    public string PreviousSigningCertificateThumbprint { get; set; } = string.Empty;

    public string SigningKeyStoreReference { get; set; } = string.Empty;

    public int MaximumClaimSize { get; set; }

    public int MaximumTokenSize { get; set; }
}
