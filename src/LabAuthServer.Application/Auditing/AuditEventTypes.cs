namespace LabAuthServer.Application.Auditing;

public static class AuditEventTypes
{
    public const string LoginSuccess = "AUTH_LOGIN_SUCCESS";
    public const string LoginFailure = "AUTH_LOGIN_FAILURE";
    public const string LdapFailure = "AUTH_LDAP_FAILURE";
    public const string AccessGranted = "AUTHZ_ACCESS_GRANTED";
    public const string AccessDenied = "AUTHZ_ACCESS_DENIED";
    public const string InvalidToken = "SEC_INVALID_TOKEN";
    public const string ExpiredToken = "SEC_EXPIRED_TOKEN";
    public const string InvalidSignature = "SEC_INVALID_SIGNATURE";
    public const string InvalidIssuer = "SEC_INVALID_ISSUER";
    public const string InvalidAudience = "SEC_INVALID_AUDIENCE";
    public const string InvalidRole = "SEC_INVALID_ROLE";
    public const string UnhandledException = "APP_UNHANDLED_EXCEPTION";
    public const string ValidationError = "APP_VALIDATION_ERROR";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        LoginSuccess, LoginFailure, LdapFailure, AccessGranted, AccessDenied,
        InvalidToken, ExpiredToken, InvalidSignature, InvalidIssuer, InvalidAudience,
        InvalidRole, UnhandledException, ValidationError
    };
}
