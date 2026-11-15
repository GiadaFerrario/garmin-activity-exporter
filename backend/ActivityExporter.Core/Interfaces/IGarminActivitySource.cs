using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Interfaces;

public interface IGarminActivitySource
{
    /// <summary>
    /// Lists the activities that started in a period. The source reads every page of the answer of Garmin.
    /// </summary>
    /// <param name="from">Start of the period, included</param>
    /// <param name="to">End of the period</param>
    /// <returns>The activities of the period, in any order</returns>
    /// <exception cref="GarminSourceException">When Garmin cannot be reached or refuses the request</exception>
    Task<IReadOnlyList<GarminActivityListItem>> ListActivitiesAsync(DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Downloads the .fit file of an activity. Garmin sends it inside a ZIP: the source opens it.
    /// </summary>
    /// <param name="garminActivityId">The id of Garmin of the activity</param>
    /// <returns>The content of the .fit file; whoever receives it disposes it</returns>
    /// <exception cref="GarminSourceException">When Garmin cannot be reached, refuses the request or the ZIP is not the expected one</exception>
    Task<Stream> DownloadFitAsync(long garminActivityId);
}
