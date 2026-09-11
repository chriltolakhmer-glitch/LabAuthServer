namespace LabAuthServer.Application.Licensing;

/// <summary>
/// Outcome of a bounded license-file read (Phase 4.16). Failures are reported as a typed
/// status rather than an exception; the reader never throws for an ordinary missing or
/// unreadable file.
/// </summary>
public enum LicenseFileReadStatus
{
    /// <summary>The file was read within the size bound.</summary>
    Success = 0,

    /// <summary>No path was configured.</summary>
    PathEmpty = 1,

    /// <summary>No file exists at the configured path.</summary>
    NotFound = 2,

    /// <summary>The file exists but could not be read.</summary>
    Unreadable = 3,

    /// <summary>The file exceeded the configured size bound and was not read.</summary>
    TooLarge = 4
}

/// <summary>Result of a bounded license-file read.</summary>
public sealed class LicenseFileReadResult
{
    private LicenseFileReadResult(LicenseFileReadStatus status, byte[] content)
    {
        Status = status;
        Content = content;
    }

    /// <summary>Typed read outcome.</summary>
    public LicenseFileReadStatus Status { get; }

    /// <summary>File bytes when <see cref="Status"/> is <see cref="LicenseFileReadStatus.Success"/>; otherwise empty.</summary>
    public byte[] Content { get; }

    /// <summary>True only when the file content is available.</summary>
    public bool Succeeded => Status == LicenseFileReadStatus.Success;

    /// <summary>Builds a successful read result.</summary>
    public static LicenseFileReadResult Success(byte[] content)
        => new(LicenseFileReadStatus.Success, content ?? throw new ArgumentNullException(nameof(content)));

    /// <summary>Builds a failed read result.</summary>
    public static LicenseFileReadResult Failed(LicenseFileReadStatus status)
        => new(status, Array.Empty<byte>());
}

/// <summary>
/// Reads a license file with a bounded size. Implementations must not read more than the
/// supplied bound and must not throw for ordinary I/O failures (Phase 4.16).
/// </summary>
public interface ILicenseFileReader
{
    /// <summary>Reads <paramref name="path"/> up to <paramref name="maximumBytes"/>.</summary>
    LicenseFileReadResult Read(string path, long maximumBytes);
}