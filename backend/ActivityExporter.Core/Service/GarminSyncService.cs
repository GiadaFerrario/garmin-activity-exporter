using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Service;

/// <summary>
/// Brings the new activities of Garmin into the history: it looks at the last days, skips what the history
/// already knows and saves the rest. Run, bike and swim are read from their .fit file (the same parser of the
/// upload), HIIT and strength are saved from what the list says.
/// </summary>
public class GarminSyncService
{
    private readonly IGarminActivitySource _source;
    private readonly IActivityRepository _repository;
    private readonly ActivityImportService _importService;
    private readonly TimeProvider _timeProvider;
    private readonly GarminSyncOptions _options;

    public GarminSyncService(
        IGarminActivitySource source,
        IActivityRepository repository,
        ActivityImportService importService,
        TimeProvider timeProvider,
        GarminSyncOptions? options = null)
    {
        _source = source;
        _repository = repository;
        _importService = importService;
        _timeProvider = timeProvider;
        _options = options ?? new GarminSyncOptions();
    }

    /// <exception cref="GarminSourceException">When the list of activities cannot be read from Garmin</exception>
    public async Task<GarminSyncResult> SyncAsync()
    {
        var to = _timeProvider.GetUtcNow();
        var from = to.AddDays(-_options.LookbackDays);

        var listed = await _source.ListActivitiesAsync(from, to);
        var known = await _repository.GetKnownGarminIdsAsync(from);

        // Newest first: if the limit of downloads is reached, the oldest wait for the next sync
        var newActivities = listed
            .Where(item => !known.Contains(item.GarminActivityId))
            .OrderByDescending(item => item.StartTime);

        var outcomes = new List<GarminSyncOutcome>();
        var downloads = 0;

        foreach (var item in newActivities)
        {
            var sport = GarminActivityTypes.ToSport(item.TypeKey);
            if (sport is null)
            {
                outcomes.Add(ToOutcome(item, null, GarminSyncStatus.Unsupported,
                    $"The activity type '{item.TypeKey}' is not supported."));
                continue;
            }

            SavedActivity activity;
            // The sports with a coach text are the ones read from their .fit file
            if (sport.Value.HasCoachText())
            {
                if (downloads >= _options.MaxDownloadsPerSync)
                {
                    outcomes.Add(ToOutcome(item, sport, GarminSyncStatus.Postponed,
                        "Not downloaded in this sync: the next one will do it."));
                    continue;
                }

                // Counted before the download: a refused download uses the limit too
                downloads++;
                var (fromFit, error) = await ReadFromFitAsync(item);
                if (fromFit is null)
                {
                    outcomes.Add(ToOutcome(item, sport, GarminSyncStatus.Error, error));
                    continue;
                }

                activity = fromFit;
            }
            else
            {
                activity = FromList(item, sport.Value);
            }

            try
            {
                var saved = await _repository.AddAsync(activity);
                outcomes.Add(ToOutcome(item, sport, GarminSyncStatus.Saved, saved: saved));
            }
            catch (DuplicateActivityException)
            {
                // It became known while we were working (for example another sync saved it): nothing to do
            }
        }

        return new GarminSyncResult(outcomes);
    }

    // Downloads the .fit file and reads it with the same service of the upload
    private async Task<(SavedActivity? Activity, string? Error)> ReadFromFitAsync(GarminActivityListItem item)
    {
        Stream fit;
        try
        {
            fit = await _source.DownloadFitAsync(item.GarminActivityId);
        }
        catch (GarminSourceException exception)
        {
            return (null, exception.Message);
        }

        await using (fit)
        {
            var result = _importService.Import([new UploadedFile($"{item.GarminActivityId}.fit", fit)]).Single();
            if (result.Status == UploadStatus.Error || result.Summary is null)
            {
                return (null, result.Message);
            }

            var summary = result.Summary;
            // The start time stays the one of the list: it is what the next syncs compare with.
            // Heart rate and calories are not in the .fit summary, they come from the list.
            var activity = new SavedActivity(
                Id: 0,
                item.GarminActivityId,
                item.Name,
                item.StartTime,
                summary.Sport,
                summary.TotalTime,
                summary.DistanceInMeters,
                summary.ElevationGain,
                item.AvgHR,
                item.MaxHR,
                item.Calories,
                summary.Splits);
            return (activity, null);
        }
    }

    private static SavedActivity FromList(GarminActivityListItem item, Sport sport)
    {
        return new SavedActivity(
            Id: 0,
            item.GarminActivityId,
            item.Name,
            item.StartTime,
            sport,
            item.Duration,
            DistanceInMeters: null,
            ElevationGain: null,
            item.AvgHR,
            item.MaxHR,
            item.Calories,
            Splits: []);
    }

    private static GarminSyncOutcome ToOutcome(
        GarminActivityListItem item,
        Sport? sport,
        GarminSyncStatus status,
        string? message = null,
        SavedActivity? saved = null)
    {
        return new GarminSyncOutcome(
            item.GarminActivityId,
            item.Name,
            item.TypeKey,
            sport,
            item.StartTime,
            item.Duration,
            status,
            message,
            saved);
    }
}
