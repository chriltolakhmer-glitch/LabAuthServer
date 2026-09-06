namespace LabAuthServer.Application.Constants;

public static class AuthorizationRoles
{
    public const string Reader = "Reader";
    public const string Operator = "Operator";
    public const string Administrator = "Administrator";

    public static IReadOnlyList<string> All { get; } = [Administrator, Operator, Reader];
}
