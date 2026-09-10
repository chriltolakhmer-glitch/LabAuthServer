namespace LabAuthServer.Api.Requests;

public static class JwtRequestSizePolicy
{
    // Measured on the direct IIS HTTP/1.1 and HTTP/2 route with ordinary
    // headers plus 3 KiB of extra metadata. Other ingress paths need validation.
    public const int MaximumEncodedJwtSize = 12288;
    public const int MaximumAuthorizationHeaderSize = 12352;

    // 804 header bytes, a 512-byte RSA signature and two separators.
    // Keep the issuance payload setting distinct from this transport budget.
    public static bool SupportsPayloadSize(int maximumTokenSize)
        => maximumTokenSize > 0 &&
           1072L + ((4L * maximumTokenSize + 2) / 3) + 683 + 2 <= MaximumEncodedJwtSize;
}
