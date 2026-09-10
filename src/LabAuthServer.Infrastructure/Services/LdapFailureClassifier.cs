using System.DirectoryServices.Protocols;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using LabAuthServer.Application.DTOs;
using LabAuthServer.Application.Enums;
using Microsoft.Extensions.Options;

namespace LabAuthServer.Infrastructure.Services;

internal static class LdapFailureClassifier
{
    public static bool IsExpected(Exception exception, DirectoryFailureStage stage)
        => exception is LdapException or DirectoryOperationException or TimeoutException or OperationCanceledException
            or SocketException or IOException or AuthenticationException or ObjectDisposedException
            or CryptographicException or UnauthorizedAccessException or OptionsValidationException or LdapResponseValidationException or BerConversionException
            || (stage == DirectoryFailureStage.CredentialLoading && exception is InvalidOperationException);

    public static DirectoryFailure Classify(Exception exception, DirectoryFailureStage stage, CancellationToken cancellationToken)
    {
        // Only cancellation-shaped failures tied to this caller count as cancelled.
        if (cancellationToken.IsCancellationRequested && exception is OperationCanceledException or ObjectDisposedException or LdapException)
            return new(AuthenticationFailureCategory.Cancelled, stage, DirectoryFailureReason.CallerCancelled);
        if (exception is OperationCanceledException)
            return new(AuthenticationFailureCategory.Unexpected, stage, DirectoryFailureReason.UnassociatedCancellation);
        if (stage == DirectoryFailureStage.CredentialLoading && exception is InvalidOperationException or IOException or UnauthorizedAccessException or CryptographicException)
            return new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.CredentialStoreUnavailable);
        if (exception is OptionsValidationException)
            return new(AuthenticationFailureCategory.Configuration, stage, DirectoryFailureReason.InvalidConfiguration);
        if (exception is LdapResponseValidationException or BerConversionException)
            return new(AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureStage.ResponseValidation, DirectoryFailureReason.InvalidResponse);
        if (exception is LdapException ldap)
            return FromCode(ldap.ErrorCode, DirectoryDiagnosticSource.LdapError, stage);
        if (exception is DirectoryOperationException operation)
            return operation.Response is null
                ? new(AuthenticationFailureCategory.Unexpected, stage, DirectoryFailureReason.MissingOperationResult)
                : FromCode((int)operation.Response.ResultCode, DirectoryDiagnosticSource.OperationResult, stage);
        if (exception is TimeoutException)
            return new(AuthenticationFailureCategory.Timeout, stage, DirectoryFailureReason.OperationTimedOut);
        if (exception is SocketException { SocketErrorCode: SocketError.TimedOut })
            return new(AuthenticationFailureCategory.Timeout, stage, DirectoryFailureReason.OperationTimedOut);
        if (IsNetworkStage(stage) && (exception is AuthenticationException or IOException || exception is SocketException socket && IsTransportError(socket.SocketErrorCode)))
            return new(AuthenticationFailureCategory.DirectoryUnavailable, stage, DirectoryFailureReason.TransportFailure);
        return Unexpected(stage);
    }

    public static DirectoryFailure FromCode(int code, DirectoryDiagnosticSource source, DirectoryFailureStage stage)
    {
        var (category, reason) = code switch
        {
            49 when stage == DirectoryFailureStage.UserBind => (AuthenticationFailureCategory.InvalidCredentials, DirectoryFailureReason.UserBindRejected),
            49 when stage == DirectoryFailureStage.ServiceBind => (AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.ServiceBindRejected),
            81 => (AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.TransportFailure),
            51 => (AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.DirectoryBusy),
            52 => (AuthenticationFailureCategory.DirectoryUnavailable, DirectoryFailureReason.DirectoryUnavailable),
            85 or 3 => (AuthenticationFailureCategory.Timeout, DirectoryFailureReason.OperationTimedOut),
            1 => (AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.OperationFailed),
            2 => (AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.ProtocolError),
            4 or 11 => (AuthenticationFailureCategory.ProtocolFailure, DirectoryFailureReason.ResultLimitExceeded),
            _ => (AuthenticationFailureCategory.Unexpected, DirectoryFailureReason.UnexpectedFailure)
        };
        return new(category, stage, reason, code, source);
    }

    public static DirectoryFailure Unexpected(DirectoryFailureStage stage)
        => new(AuthenticationFailureCategory.Unexpected, stage, DirectoryFailureReason.UnexpectedFailure);

    private static bool IsNetworkStage(DirectoryFailureStage stage)
        => stage is DirectoryFailureStage.ConnectionSetup or DirectoryFailureStage.ServiceBind
            or DirectoryFailureStage.UserSearch or DirectoryFailureStage.UserBind or DirectoryFailureStage.GroupSearch;

    private static bool IsTransportError(SocketError error)
        => error is SocketError.ConnectionRefused or SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData
            or SocketError.NoRecovery or SocketError.NetworkUnreachable or SocketError.HostUnreachable or SocketError.NetworkDown
            or SocketError.ConnectionReset or SocketError.ConnectionAborted or SocketError.NotConnected or SocketError.Shutdown;
}
