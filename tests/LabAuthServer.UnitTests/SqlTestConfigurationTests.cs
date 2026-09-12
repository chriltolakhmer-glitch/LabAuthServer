namespace LabAuthServer.UnitTests;

public sealed class SqlTestConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("0")]
    public void TargetAloneNeverEnablesInfrastructure(string? enabled)
    {
        string? Read(string name) => name == SqlTestConfiguration.EnableVariable ? enabled : "explicit-test-target";
        Assert.False(SqlTestConfiguration.IsEnabled(Read));
        Assert.Throws<InvalidOperationException>(() => SqlTestConfiguration.RequireConnectionString(Read));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnabledWithoutTargetFailsInsteadOfFallingBack(string? target)
    {
        string? Read(string name) => name == SqlTestConfiguration.EnableVariable ? "1" : target;
        var exception = Assert.Throws<InvalidOperationException>(() => SqlTestConfiguration.RequireConnectionString(Read));
        Assert.Contains(SqlTestConfiguration.ConnectionVariable, exception.Message);
    }

    [Fact]
    public void EnabledUsesOnlyTheExactSuppliedConnection()
    {
        const string supplied = "Server=(localdb)\\ExplicitTestInstance;Database=ExplicitTestDatabase;Integrated Security=True";
        string? Read(string name) => name == SqlTestConfiguration.EnableVariable ? "1" : supplied;
        Assert.Equal(supplied, SqlTestConfiguration.RequireConnectionString(Read));
    }
}
