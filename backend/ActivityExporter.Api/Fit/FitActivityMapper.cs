using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using Dynastream.Fit;
using CoreSport = ActivityExporter.Core.Models.Sport;
using FitSport = Dynastream.Fit.Sport;
namespace ActivityExporter.Api;

public class FitActivityMapper
{
    private const int MinLapDurationInSeconds = 3;
    
    public static ActivitySummary Map(SessionMesg session, IReadOnlyList<LapMesg> laps)
    {
        // ActivitySummary (int Id, DateTimeOffset StartTime, Sport Sport, TimeSpan TotalTime, double DistanceInMeters, int? ElevationGain, IReadOnlyList<Split> Splits)
        // mapped once: it rejects the unsupported sports and is needed by both the splits and the summary
        var sport = MapSport(session.GetSport());

        IReadOnlyList<Split> splits = laps
            .Where(lap => IsWorkLap(lap))
            .Select(lap => MapLap(lap, sport)).ToList();
        var dateTime = session.GetStartTime()?.GetDateTime();
        if (dateTime == null)
        {
            throw new InvalidFileException("The file has no start time.");
        }
        var startTime = new DateTimeOffset(dateTime.Value, TimeSpan.Zero);
        float? time = session.GetTotalTimerTime();
        ActivitySummary activitySummary = new ActivitySummary(0,
            startTime,
            sport,
            TimeSpan.FromSeconds(time ?? 0),
            session.GetTotalDistance() ?? 0,
            session.GetTotalAscent(),
            splits);
        return activitySummary;
    }

    private static Split MapLap(LapMesg lap, CoreSport sport)
    {
        // one lap -> one Split (TimeSpan Time, double DistanceInMeters, int? AvgHR,int? MaxHR)
        float? time = lap.GetTotalTimerTime();
        bool isBike = sport == CoreSport.Bike;
        return new Split(
            TimeSpan.FromSeconds(time ?? 0), 
            lap.GetTotalDistance() ?? 0, // lap total distance already in meters
            ToHeartRate(lap.GetAvgHeartRate()), ToHeartRate(lap.GetMaxHeartRate()),
            AvgCadence: isBike ? lap.GetAvgCadence() : null,
            MaxCadence: isBike ? lap.GetMaxCadence() : null,
            AvgPower: isBike ? lap.GetAvgPower() : null,
            MaxPower: isBike ? lap.GetMaxPower() : null
            ); 
    }

    // Without a heart-rate strap the watch writes 0 instead of leaving the field out, and a heart rate of 0
    // does not exist: it means "missing" (the text then shows a dash)
    private static int? ToHeartRate(byte? rate)
    {
        return rate > 0 ? rate : null;
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

    private static bool IsWorkLap(LapMesg lap)
    {
        return (lap.GetIntensity() is null or Intensity.Active) && lap.GetTotalDistance() > 0 && lap.GetTotalTimerTime() > MinLapDurationInSeconds;
    }
}