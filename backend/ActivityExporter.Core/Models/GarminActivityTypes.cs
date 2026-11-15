namespace ActivityExporter.Core.Models;

/// <summary>
/// Which of our sports an activity type of Garmin is. The table is explicit on purpose: the "parent type"
/// of Garmin is not reliable (walking and hiking have the same parent as running).
/// The keys are the ones seen in real data; a type that is not here is simply not supported yet.
/// </summary>
public static class GarminActivityTypes
{
    /// <param name="typeKey">The type key of Garmin, for example "running"</param>
    /// <returns>The sport, or null if the type is not supported</returns>
    public static Sport? ToSport(string typeKey) => typeKey switch
    {
        "running" or "treadmill_running" => Sport.Run,
        "gravel_cycling" or "virtual_ride" or "indoor_cycling" or "road_biking" => Sport.Bike,
        "lap_swimming" or "open_water_swimming" => Sport.Swim,
        "hiit" => Sport.Hiit,
        "strength_training" => Sport.Strength,
        _ => null
    };
}
