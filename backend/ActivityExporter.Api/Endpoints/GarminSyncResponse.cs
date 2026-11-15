using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

/// <param name="SavedId">The id of the history when the activity was saved; null otherwise</param>
public record GarminSyncOutcomeResponse(
    long GarminActivityId,
    string Name,
    string TypeKey,
    Sport? Sport,
    DateTimeOffset StartTime,
    double DurationInSeconds,
    GarminSyncStatus Status,
    string? Message,
    int? SavedId);

/// <param name="PostponedCount">How many activities wait for the next sync because of the limit of downloads</param>
public record GarminSyncResponse(IReadOnlyList<GarminSyncOutcomeResponse> Outcomes, int PostponedCount);
