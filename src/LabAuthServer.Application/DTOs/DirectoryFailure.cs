using LabAuthServer.Application.Enums;

namespace LabAuthServer.Application.DTOs;

/// <summary>Bounded diagnostics; never contains directory or exception messages.</summary>
public sealed class DirectoryFailure
{
    public DirectoryFailure(AuthenticationFailureCategory category, DirectoryFailureStage stage,
        DirectoryFailureReason reason, int? diagnosticCode = null,
        DirectoryDiagnosticSource diagnosticSource = DirectoryDiagnosticSource.None)
    {
        if (!Enum.IsDefined(category) || category == AuthenticationFailureCategory.None)
            throw new ArgumentOutOfRangeException(nameof(category));
        if (!Enum.IsDefined(stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (!Enum.IsDefined(diagnosticSource) || diagnosticCode.HasValue == (diagnosticSource == DirectoryDiagnosticSource.None))
            throw new ArgumentException("A numeric diagnostic requires an explicit source.", nameof(diagnosticSource));
        Category = category;
        Stage = stage;
        Reason = reason;
        DiagnosticCode = diagnosticCode;
        DiagnosticSource = diagnosticSource;
    }

    public AuthenticationFailureCategory Category { get; }
    public DirectoryFailureStage Stage { get; }
    public DirectoryFailureReason Reason { get; }
    public int? DiagnosticCode { get; }
    public DirectoryDiagnosticSource DiagnosticSource { get; }

    public string SafeMessage => Category switch
    {
        AuthenticationFailureCategory.InvalidRequest or AuthenticationFailureCategory.InvalidCredentials => "Authentication failed.",
        AuthenticationFailureCategory.DirectoryUnavailable or AuthenticationFailureCategory.ProtocolFailure or AuthenticationFailureCategory.Configuration or AuthenticationFailureCategory.ResourceExhausted => "Authentication service unavailable.",
        AuthenticationFailureCategory.Timeout => "Authentication request timed out.",
        AuthenticationFailureCategory.Cancelled => "Authentication request was cancelled.",
        _ => "Authentication error."
    };

    public DirectoryFailure AtStage(DirectoryFailureStage stage)
        => new(Category, stage, Reason, DiagnosticCode, DiagnosticSource);
}
