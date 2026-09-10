namespace LabAuthServer.Application.Enums;

public enum AuthenticationFailureCategory
{
    None = 0,
    InvalidCredentials = 1,
    DirectoryUnavailable = 2,
    Timeout = 3,
    InvalidRequest = 4,
    Configuration = 5,
    Cancelled = 6,
    Unexpected = 7,
    ProtocolFailure = 8,
    ResourceExhausted = 9
}
