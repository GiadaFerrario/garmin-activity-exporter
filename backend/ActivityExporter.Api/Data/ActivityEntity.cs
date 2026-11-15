using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

/// <summary>
/// How an activity of the history is stored. It is separate from the Core model SavedActivity
/// because the database needs things the model must not know about (soft delete, UTC dates as DateTime,
/// a List instead of an IReadOnlyList for the splits).
/// </summary>
public class ActivityEntity
{
    public int Id { get; set; }
    public long GarminActivityId { get; set; }
    public string Name { get; set; } = "";

    // DateTime in UTC and not DateTimeOffset: the SQLite provider of EF cannot order or compare DateTimeOffset
    public DateTime StartTimeUtc { get; set; }
    public Sport Sport { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double? DistanceInMeters { get; set; }
    public int? ElevationGain { get; set; }
    public int? AvgHR { get; set; }
    public int? MaxHR { get; set; }
    public int? Calories { get; set; }

    // Stored as one JSON column of the activity row (see ActivityDbContext)
    public List<Split> Splits { get; set; } = [];

    // Soft delete: null = the activity is in the history
    public DateTime? DeletedAtUtc { get; set; }
}
