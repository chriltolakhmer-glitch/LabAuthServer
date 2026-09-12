namespace LabAuthServer.UnitTests;

public sealed class SqlInfrastructureFactAttribute : FactAttribute
{
    public SqlInfrastructureFactAttribute()
    {
        if (!SqlTestConfiguration.IsEnabled(Environment.GetEnvironmentVariable))
            Skip = $"SQL infrastructure is opt-in: set {SqlTestConfiguration.EnableVariable}=1 and {SqlTestConfiguration.ConnectionVariable} to an authorized disposable target.";
    }
}
