namespace LabAuthServer.Application.Enums;

public enum DirectoryFailureStage
{
    CredentialLoading,
    ConnectionSetup,
    ServiceBind,
    UserSearch,
    UserBind,
    GroupSearch,
    ResponseValidation,
    RoleMapping,
    TokenIssuance,
    ConcurrencyWait
}
