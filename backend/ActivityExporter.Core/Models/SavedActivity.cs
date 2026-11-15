namespace ActivityExporter.Core.Models;

public record SavedActivity(
    int Id,                          // db key
    long GarminActivityId,           // Garmin id, the only way to recognize an already known activity
    string Name,
    DateTimeOffset StartTime,        // UTC
    Sport Sport,
    TimeSpan TotalTime,
    double? DistanceInMeters,        // nullable: HIIT and Strength do not have distance
    int? ElevationGain,
    int? AvgHR,
    int? MaxHR,
    int? Calories,
    IReadOnlyList<Split> Splits);    // empty for HIIT and Strength