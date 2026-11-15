using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

/// <summary>
/// An activity of the history as the frontend receives it.
/// The list does not carry the text (it is null); the detail of one activity does, when the sport has one.
/// </summary>
public record HistoryActivityResponse(
    int Id,
    long GarminActivityId,
    string Name,
    Sport Sport,
    DateTimeOffset StartTime,
    double DurationInSeconds,
    double? DistanceInMeters,
    int? ElevationGain,
    int? AvgHR,
    int? MaxHR,
    int? Calories,
    bool HasCoachText,
    string? Text);
