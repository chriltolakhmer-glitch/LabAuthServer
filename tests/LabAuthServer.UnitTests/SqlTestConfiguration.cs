namespace LabAuthServer.UnitTests;

internal static class SqlTestConfiguration
{
    public const string EnableVariable = "LABAUTHSERVER_RUN_SQL_TESTS";
    public const string ConnectionVariable = "LABAUTHSERVER_SQL_AUDIT_TEST_CONNECTION";

    public static bool IsEnabled(Func<string, string?> readEnvironment)
        => string.Equals(readEnvironment(EnableVariable), "1", StringComparison.Ordinal);

    public static string RequireConnectionString(Func<string, string?> readEnvironment)
    {
        if (!IsEnabled(readEnvironment))
            throw new InvalidOperationException($"Explicit SQL validation requires {EnableVariable}=1.");

        var connection = readEnvironment(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"{ConnectionVariable} must identify an authorized disposable SQL test target. There is no default target.");

        return connection;
    }
}
