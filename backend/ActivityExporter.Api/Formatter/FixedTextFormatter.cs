using System.Globalization;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

public class FixedTextFormatter : IActivityFormatter
{
    public string Format(ActivitySummary activitySummary)
    {
        var sport = activitySummary.Sport;
        var headerPace = sport switch
        {
            Sport.Swim => "(min/100m)",
            Sport.Bike => "(km/h)",
            Sport.Run => "(min/km)"
        };

        var bikeHeader = sport.Equals(Sport.Bike) ? $"Elevation gain: {activitySummary.ElevationGain}" : "";
        // cadence and power columns exist only for the bike
        var bikeColumns = sport == Sport.Bike ? ", avgCad, maxCad, avgPower, maxPower" : "";
        var header = $"Distance, time, pace {headerPace}, avgHR, maxHR{bikeColumns}";
        if (bikeHeader != "")
        {
            header = string.Join("\n", bikeHeader, header);
        }

        var splitLines = activitySummary.Splits.Select(split => FormatSplit(split, sport));
        var lines = splitLines.Prepend(header);
        return string.Join("\n", lines);
    }

    private string FormatSplit(Split split, Sport sport)
    {
        var distance = FormatDistance(split.DistanceInMeters);
        var time = FormatTime(split.Time);
        var pace = ComputeAndFormatPace(split.Time, split.DistanceInMeters, sport);
        var columns = new List<string>
        {
            distance, time, pace,
            FormatOptional(split.AvgHR), FormatOptional(split.MaxHR)
        };
        if (sport == Sport.Bike)
        {
            columns.Add(FormatOptional(split.AvgCadence));
            columns.Add(FormatOptional(split.MaxCadence));
            columns.Add(FormatOptional(split.AvgPower));
            columns.Add(FormatOptional(split.MaxPower));
        }

        return string.Join(" ", columns);
    }

    // A value missing from the file (example: no sensor) is written as a dash
    private static string FormatOptional(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "-";
    }

    private static string FormatDistance(double distanceInMeters)
    {
        if (distanceInMeters >= 1000)
        {
            var km = distanceInMeters / 1000;
            return $"{km.ToString("0.#", CultureInfo.InvariantCulture)}km";
        }

        return $"{distanceInMeters.ToString("0", CultureInfo.InvariantCulture)}m";
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours != 0 ? time.ToString("hh':'mm':'ss") : time.ToString("mm':'ss");
    }

    private static string ComputeAndFormatPace(TimeSpan Time, double DistanceInMeters, Sport Sport)
    {

        String pace = Sport switch
        {
            Sport.Swim => FormatPacePerDistance(Time, DistanceInMeters, 100),
            Sport.Bike => ComputeAndFormatPace_Bike(Time, DistanceInMeters),
            Sport.Run => FormatPacePerDistance(Time, DistanceInMeters, 1000),
        };

        return pace;
    }
    

    private static string ComputeAndFormatPace_Bike(TimeSpan Time, double DistanceInMeters)
    {
        // km/h: kilometres covered divided by hours elapsed
        double kmh = (DistanceInMeters / 1000) / Time.TotalHours;

        return $"@{kmh.ToString("0.0", CultureInfo.InvariantCulture)}";
    }

    // Pace as @m:ss, expressed per `paceDistanceInMeters` (1000 for run, 100 for swim)
    private static string FormatPacePerDistance(TimeSpan time, double distanceInMeters, double paceDistanceInMeters)
    {
        // time to min
        double minutes = time.TotalMinutes;
        // distance in units of the pace distance (1 unit = 1 km for run, 100 m for swim)
        double distanceUnits = distanceInMeters / paceDistanceInMeters;
        // pace min per unit
        double pace = minutes / distanceUnits;

        int min = (int)pace;
        int sec = (int)Math.Round((pace - min) * 60);

        if (sec == 60)
        {
            min++;
            sec = 0;
        }

        return $"@{min}:{sec:D2}";
    }
}
