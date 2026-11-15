namespace ActivityExporter.Core.Models;

/// <summary>
/// One row of the list of activities of Garmin: enough to know what it is and to save the sports without a .fit
/// </summary>
/// <param name="TypeKey">The activity type of Garmin as it is, for example "running" or "lap_swimming"</param>
/// <param name="StartTime">When the activity started, in UTC</param>
/// <param name="Duration">Garmin's "duration"</param>
public record GarminActivityListItem(
    long GarminActivityId,
    string Name,
    string TypeKey,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    int? AvgHR,
    int? MaxHR,
    int? Calories);
