using LabAuthServer.Application.Licensing;

namespace LabAuthServer.Infrastructure.Security.Licensing;

/// <summary>
/// Reads a license file with a bounded size (Phase 4.16). The bound is checked from file
/// metadata before any content is read, and again against the bytes actually read, so an
/// oversized or growing file is never buffered unbounded. Ordinary I/O failures are
/// reported as typed statuses rather than thrown.
/// </summary>
public sealed class BoundedLicenseFileReader : ILicenseFileReader
{
    /// <inheritdoc />
    public LicenseFileReadResult Read(string path, long maximumBytes)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LicenseFileReadResult.Failed(LicenseFileReadStatus.PathEmpty);
        }

        if (maximumBytes <= 0)
        {
            return LicenseFileReadResult.Failed(LicenseFileReadStatus.TooLarge);
        }

        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return LicenseFileReadResult.Failed(LicenseFileReadStatus.NotFound);
            }

            // Reject on declared length before reading any content.
            if (info.Length > maximumBytes)
            {
                return LicenseFileReadResult.Failed(LicenseFileReadStatus.TooLarge);
            }

            // Read with a hard cap one byte above the bound so growth is detected.
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            var capacity = (int)Math.Min(info.Length, maximumBytes);
            using var buffer = new MemoryStream(capacity);
            var chunk = new byte[4096];
            long total = 0;

            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                total += read;
                if (total > maximumBytes)
                {
                    return LicenseFileReadResult.Failed(LicenseFileReadStatus.TooLarge);
                }

                buffer.Write(chunk, 0, read);
            }

            return LicenseFileReadResult.Success(buffer.ToArray());
        }
        catch (IOException)
        {
            return LicenseFileReadResult.Failed(LicenseFileReadStatus.Unreadable);
        }
        catch (UnauthorizedAccessException)
        {
            return LicenseFileReadResult.Failed(LicenseFileReadStatus.Unreadable);
        }
    }
}