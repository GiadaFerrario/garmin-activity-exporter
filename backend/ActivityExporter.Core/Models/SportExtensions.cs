namespace ActivityExporter.Core.Models;

public static class SportExtensions
{
    public static bool HasCoachText(this Sport sport)
    {
        return sport switch
        {
            Sport.Swim or Sport.Bike or Sport.Run => true,
            Sport.Hiit or Sport.Strength => false,
            // a Sport added to the enum and not decided here: HasCoachText_EverySport_IsDecided fails
            _ => throw new ArgumentOutOfRangeException(nameof(sport), sport, "Sport not decided in HasCoachText")
            
        };
    }
}