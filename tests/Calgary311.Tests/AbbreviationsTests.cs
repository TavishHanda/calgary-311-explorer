using Calgary311.Web.Services;

namespace Calgary311.Tests;

public class AbbreviationsTests
{
    [Theory]
    [InlineData("WRS - Cart Management", "Waste & Recycling Services: Cart Management")]
    [InlineData("OS - Waste and Recycling Services", "Operational Services: Waste and Recycling Services")]
    [InlineData("Roads - Pothole Repair", "Roads - Pothole Repair")] // "Roads" is a word, not an abbreviation
    [InlineData("UEP - Odour Inquiries", "UEP - Odour Inquiries")] // unconfirmed codes are left alone
    [InlineData("BIA Requests", "BIA Requests")] // no dash at all
    public void Describe_SpellsOutOnlyKnownCodes(string name, string expected)
    {
        Assert.Equal(expected, Abbreviations.Describe(name));
    }

    [Fact]
    public void Split_KeepsDashesInTheRestOfTheName()
    {
        Assert.Equal(("Parks", "Cemetery - Maintenance - GIS"), Abbreviations.Split("Parks - Cemetery - Maintenance - GIS"));
    }

    [Fact]
    public void Legend_ListsEachKnownCodeOnce_Sorted()
    {
        var legend = Abbreviations.Legend(["WRS - Bin Checks", "WRS - Cart Management", "AS - Animal Bite", "Roads - Pothole Repair", null]);

        Assert.Equal(["AS", "WRS"], legend.Select(item => item.Key));
        Assert.Equal("Animal Services", legend[0].Value);
    }
}
