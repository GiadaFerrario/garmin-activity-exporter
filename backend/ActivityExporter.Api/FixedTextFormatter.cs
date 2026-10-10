using System.Globalization;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

public class FixedTextFormatter : IActivityFormatter
{
    public string Format(ActivitySummary activitySummary)
    {   
        var sport = activitySummary.Sport;
        var header = "Distance, time, pace (min/km), avgHR, maxHR"; // TODO depends on sport

        var splitLines = activitySummary.Splits.Select(FormatSplit);
        var lines = splitLines.Prepend(header);
        return string.Join("\n", lines);
    }

    private string FormatSplit(Split split)
    {
        var distance = FormatDistance(split.DistanceInMeters);
        var time = FormatTime(split.Time);
        var pace = ComputeAndFormatPace(split.Time, split.DistanceInMeters);
        var avgHR = split.AvgHR.ToString();
        var maxHR = split.MaxHR.ToString();
        var splitText = string.Join(" ", distance, time, pace, avgHR, maxHR);
        return splitText;
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

    private static string ComputeAndFormatPace(TimeSpan Time, double DistanceInMeters)
    {
        // time to min
        double minutes = Time.TotalMinutes;
        // distance to km
        double distanceKm = DistanceInMeters / 1000;
        // pace min/km
        double pace = minutes / distanceKm;
            
            
        int min = (int)pace;
        int sec = (int)Math.Round((pace - min) * 60);

        if (sec == 60)
        {
            min++;
            sec = 0;
        }

        string strPace = $"@{min}:{sec:D2}";
        
        return strPace;
    }
}