using ActivityExporter.Api;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Tests;

public class FixedTextFormatterTests
{
    [Fact]
    public void Format_RunWithTwoSplits()
    {
        var split1 = new Split(new TimeSpan(0, 5, 00), 1000, 160, 178);
        var split2 = new Split(new TimeSpan(0, 3, 00), 1000, 175, 190);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 8, 0), 
            DistanceInMeters: split1.DistanceInMeters + split2.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1, split2]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            1km 05:00 @5:00 160 178
            1km 03:00 @3:00 175 190
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_SplitUnderOneKm_ShowsDistanceInMeters()
    {
        var split1 = new Split(new TimeSpan(0, 5, 00), 1200, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 5, 0), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            1.2km 05:00 @4:10 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_SplitOverOneKm_ShowsDistanceInKm()
    {
        var split1 = new Split(new TimeSpan(0, 5, 00), 800, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 5, 0), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            800m 05:00 @6:15 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_PaceWithFractionalSeconds_RoundsToNearestSecond()
    {
        var split1 = new Split(new TimeSpan(0, 20, 00), 1700, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 20, 0), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            1.7km 20:00 @11:46 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
}