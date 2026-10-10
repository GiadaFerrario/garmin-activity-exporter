using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using Dynastream.Fit;
using CoreSport = ActivityExporter.Core.Models.Sport;
using FitSport = Dynastream.Fit.Sport;
namespace ActivityExporter.Api;

public class FitActivityMapper
{
    public static ActivitySummary Map(SessionMesg session, IReadOnlyList<LapMesg> laps)
    {
        // ActivitySummary (int Id, DateTimeOffset StartTime, Sport Sport, TimeSpan TotalTime, double DistanceInMeters, int? ElevationGain, IReadOnlyList<Split> Splits)
        IReadOnlyList<Split> splits = laps.Select(MapLap).ToList();
        var dateTime = session.GetStartTime()?.GetDateTime();
        if (dateTime == null)
        {
            throw new InvalidFileException("The file has no start time.");
        }
        var startTime = new DateTimeOffset(dateTime.Value, TimeSpan.Zero);
        float? time = session.GetTotalTimerTime();
        ActivitySummary activitySummary = new ActivitySummary(0,
            startTime,
            MapSport(session.GetSport()),
            TimeSpan.FromSeconds(time ?? 0),
            session.GetTotalDistance() ?? 0,
            session.GetTotalAscent(),
            splits);
        return activitySummary;
    }

    private static Split MapLap(LapMesg lap)
    {
        // one lap -> one Split (TimeSpan Time, double DistanceInMeters, int? AvgHR,int? MaxHR)
        float? time = lap.GetTotalTimerTime();
        return new Split(TimeSpan.FromSeconds(time ?? 0), lap.GetTotalDistance() ?? 0, lap.GetAvgHeartRate(), lap.GetMaxHeartRate()); // lap total distance already in meters
    }

    private static CoreSport MapSport(FitSport? sport)
    {
        // Running/Cycling/Swimming -> Run/Bike/Swim, anything else -> InvalidFileException
        return sport switch
        {
            FitSport.Running => CoreSport.Run,
            FitSport.Cycling => CoreSport.Bike,
            FitSport.Swimming => CoreSport.Swim,
            _ => throw new InvalidFileException($"{(sport.HasValue ? sport.ToString() : "The uploaded sport")} is not a supported Sport.")
        };
    }
}