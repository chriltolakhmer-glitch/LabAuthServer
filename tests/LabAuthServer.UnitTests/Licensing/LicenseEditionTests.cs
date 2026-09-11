using LabAuthServer.Domain.Licensing;

namespace LabAuthServer.UnitTests.Licensing;

public sealed class LicenseEditionTests
{
    [Theory]
    [InlineData("Community", LicenseEdition.Community)]
    [InlineData("community", LicenseEdition.Community)]
    [InlineData("Professional", LicenseEdition.Professional)]
    [InlineData("Enterprise", LicenseEdition.Enterprise)]
    [InlineData("  Enterprise  ", LicenseEdition.Enterprise)]
    public void ParseEdition_RecognizesApprovedEditions(string value, LicenseEdition expected)
    {
        Assert.Equal(expected, LicenseEditionExtensions.ParseEdition(value));
        Assert.True(expected.IsKnown());
    }

    [Theory]
    [InlineData("Ultimate")]
    [InlineData("community-plus")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseEdition_UnknownEditionMapsToUnknown(string? value)
    {
        var edition = LicenseEditionExtensions.ParseEdition(value);

        Assert.Equal(LicenseEdition.Unknown, edition);
        Assert.False(edition.IsKnown());
    }

    [Fact]
    public void Rank_OrdersEditionsAndUnknownIsBelowAll()
    {
        Assert.True(LicenseEdition.Community.Rank() < LicenseEdition.Professional.Rank());
        Assert.True(LicenseEdition.Professional.Rank() < LicenseEdition.Enterprise.Rank());
        Assert.Equal(-1, LicenseEdition.Unknown.Rank());
    }
}