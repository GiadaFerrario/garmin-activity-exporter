using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Service;

/// <summary>
/// Gives an activity of the history together with its coach text, which is never stored: it is
/// written again every time, so a change of the output format reaches the old activities too.
/// </summary>
public class ActivityHistoryService
{
    private readonly IActivityRepository _repository;
    private readonly IActivityFormatter _formatter;

    public ActivityHistoryService(IActivityRepository repository, IActivityFormatter formatter)
    {
        _repository = repository;
        _formatter = formatter;
    }

    /// <returns>The activity with its text (null text for the sports without one), or null if the id is unknown or deleted</returns>
    public async Task<HistoryDetail?> GetDetailAsync(int id)
    {
        var activity = await _repository.GetAsync(id);
        if (activity is null)
        {
            return null;
        }

        var text = activity.Sport.HasCoachText() ? _formatter.Format(ToSummary(activity)) : null;
        return new HistoryDetail(activity, text);
    }

    // The formatter knows ActivitySummary, the thing that the parser produces: the saved activity is turned back into it
    private static ActivitySummary ToSummary(SavedActivity activity)
    {
        return new ActivitySummary(
            activity.Id,
            activity.StartTime,
            activity.Sport,
            activity.TotalTime,
            activity.DistanceInMeters ?? 0,
            activity.ElevationGain,
            activity.Splits);
    }
}
