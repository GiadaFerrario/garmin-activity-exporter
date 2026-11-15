namespace ActivityExporter.Core.Models;

/// <summary>
/// What happened to one new activity of Garmin during a sync
/// </summary>
/// <param name="Sport">Null when the type of Garmin is not supported</param>
/// <param name="Message">Why it was not saved; null when it was saved</param>
/// <param name="Saved">The activity as it is in the history; null when it was not saved</param>
public record GarminSyncOutcome(
    long GarminActivityId,
    string Name,
    string TypeKey,
    Sport? Sport,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    GarminSyncStatus Status,
    string? Message = null,
    SavedActivity? Saved = null);
