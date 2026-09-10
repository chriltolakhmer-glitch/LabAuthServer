using System.Security.Cryptography;

namespace LabAuthServer.Infrastructure.Security;

public static class RsaKeySizePolicy
{
    public const int MinimumRsaKeySize = 2048;
    public const int MaximumRsaKeySize = 4096;

    public static void Validate(RSA key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.KeySize < MinimumRsaKeySize)
            throw new InvalidOperationException("The RSA key is too small.");
        if (key.KeySize > MaximumRsaKeySize)
            throw new InvalidOperationException("The RSA key is too large.");
    }
}
