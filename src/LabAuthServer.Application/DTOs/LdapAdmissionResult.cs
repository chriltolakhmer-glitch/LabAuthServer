namespace LabAuthServer.Application.DTOs;

/// <summary>Either owns one LDAP permit or carries a typed admission failure, never both.</summary>
public sealed class LdapAdmissionResult : IDisposable
{
    private IDisposable? _lease;

    private LdapAdmissionResult(IDisposable? lease, DirectoryFailure? failure)
    {
        _lease = lease;
        Failure = failure;
    }

    public bool IsSuccess => Failure is null;
    public DirectoryFailure? Failure { get; }

    public static LdapAdmissionResult Succeeded(IDisposable lease)
        => new(lease ?? throw new ArgumentNullException(nameof(lease)), null);

    public static LdapAdmissionResult Failed(DirectoryFailure failure)
        => new(null, failure ?? throw new ArgumentNullException(nameof(failure)));

    public void Dispose() => Interlocked.Exchange(ref _lease, null)?.Dispose();
}
