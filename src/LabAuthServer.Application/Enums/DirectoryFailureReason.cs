namespace LabAuthServer.Application.Enums;

public enum DirectoryFailureReason
{
    InvalidInput,
    InvalidConfiguration,
    CredentialStoreUnavailable,
    UserBindRejected,
    ServiceBindRejected,
    AccountDisabled,
    TransportFailure,
    DirectoryBusy,
    DirectoryUnavailable,
    OperationTimedOut,
    CallerCancelled,
    UnassociatedCancellation,
    OperationFailed,
    ProtocolError,
    ResultLimitExceeded,
    MissingOperationResult,
    InvalidResponse,
    UnexpectedEntryCount,
    InvalidIdentityData,
    InvalidGroupData,
    IncompleteMembership,
    UnexpectedFailure,
    AuthenticationDeadlineExceeded,
    PendingWaiterCapacityExceeded,
    MembershipLimitExceeded
}
