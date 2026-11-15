namespace ActivityExporter.Core.Models;

/// <summary>
/// The outcome of every new activity found by a sync, the most recent first.
/// The activities already in the history are not listed.
/// </summary>
public record GarminSyncResult(IReadOnlyList<GarminSyncOutcome> Outcomes)
{
    public int PostponedCount => Outcomes.Count(outcome => outcome.Status == GarminSyncStatus.Postponed);
}
