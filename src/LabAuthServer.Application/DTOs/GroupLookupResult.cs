namespace LabAuthServer.Application.DTOs;

/// <summary>A complete membership result or a failure, never both.</summary>
public sealed class GroupLookupResult
{
    private readonly IReadOnlyList<string>? _groups;
    private GroupLookupResult(IReadOnlyList<string>? groups, DirectoryFailure? failure)
    {
        _groups = groups;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public DirectoryFailure? Failure { get; }
    public IReadOnlyList<string> Groups => _groups ?? throw new InvalidOperationException("A failed lookup has no membership result.");

    public static GroupLookupResult Succeeded(IReadOnlyList<string> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        return new(Array.AsReadOnly(groups.ToArray()), null);
    }

    public static GroupLookupResult Failed(DirectoryFailure failure)
        => new(null, failure ?? throw new ArgumentNullException(nameof(failure)));
}
