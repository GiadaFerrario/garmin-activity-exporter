using ActivityExporter.Core.Models;

namespace ActivityExporter.Tests;

public class SportExtensionsTests
{
    [Theory]
    [InlineData(Sport.Run)]
    [InlineData(Sport.Bike)]
    [InlineData(Sport.Swim)]
    public void HasCoachText_RunBikeSwim_ReturnsTrue(Sport sport)
    {
        Assert.True(sport.HasCoachText());
    }

    [Theory]
    [InlineData(Sport.Hiit)]
    [InlineData(Sport.Strength)]
    public void HasCoachText_HiitAndStrength_ReturnsFalse(Sport sport)
    {
        Assert.False(sport.HasCoachText());
    }

    // Safety net: whoever adds a Sport to the enum must decide if it has a coach text,
    // otherwise HasCoachText throws for it and this test fails
    [Fact]
    public void HasCoachText_EverySport_IsDecided()
    {
        Assert.All(Enum.GetValues<Sport>(), sport => sport.HasCoachText());
    }
}
