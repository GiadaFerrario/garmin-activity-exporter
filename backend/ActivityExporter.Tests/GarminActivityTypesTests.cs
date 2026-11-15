using ActivityExporter.Core.Models;

namespace ActivityExporter.Tests;

public class GarminActivityTypesTests
{
    // The type keys of Garmin that the author's own activities use (checked on real data)
    [Theory]
    [InlineData("running", Sport.Run)]
    [InlineData("treadmill_running", Sport.Run)]
    [InlineData("gravel_cycling", Sport.Bike)]
    [InlineData("virtual_ride", Sport.Bike)]
    [InlineData("indoor_cycling", Sport.Bike)]
    [InlineData("road_biking", Sport.Bike)]
    [InlineData("lap_swimming", Sport.Swim)]
    [InlineData("open_water_swimming", Sport.Swim)]
    [InlineData("hiit", Sport.Hiit)]
    [InlineData("strength_training", Sport.Strength)]
    public void ToSport_SupportedType_ReturnsTheSport(string typeKey, Sport expected)
    {
        Assert.Equal((Sport?)expected, GarminActivityTypes.ToSport(typeKey));
    }

    [Theory]
    [InlineData("walking")]
    [InlineData("hiking")]
    [InlineData("mobility")]
    [InlineData("indoor_climbing")]
    [InlineData("a_type_that_does_not_exist")]
    [InlineData("")]
    public void ToSport_OtherType_ReturnsNull(string typeKey)
    {
        Assert.Null(GarminActivityTypes.ToSport(typeKey));
    }
}
