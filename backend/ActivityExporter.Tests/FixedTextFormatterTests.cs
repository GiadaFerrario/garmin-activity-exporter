using ActivityExporter.Api;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Tests;

public class FixedTextFormatterTests
{
    
    [Fact]
    public void Format_Run()
    {
        var split1 = new Split(new TimeSpan(0, 5, 00), 1000, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 8, 0), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            1km 05:00 @5:00 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
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
    public void Format_Swim()
    {
        var split1 = new Split(new TimeSpan(0, 1, 40), 50, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Swim,
            TotalTime: new TimeSpan(0, 1, 40), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/100m), avgHR, maxHR
            50m 01:40 @3:20 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_Bike()
    {
        var split1 = new Split(new TimeSpan(1, 19, 34), 20632.83, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Bike,
            TotalTime: new TimeSpan(1, 19, 34), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: 200,
            Splits: [split1]
        );

        var text =
            """
            Elevation gain: 200
            Distance, time, pace (km/h), avgHR, maxHR, avgCad, maxCad, avgPower, maxPower
            20.6km 01:19:34 @15.6 160 178 - - - -
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();

        var result = formatter.Format(summary);

        Assert.Equal(text, result);
    }

    [Fact]
    public void Format_BikeWithCadenceAndPower_AddsFourColumns()
    {
        var split = new Split(new TimeSpan(0, 30, 0), 10000, 150, 170,
            AvgCadence: 82, MaxCadence: 95, AvgPower: 160, MaxPower: 280);

        var text =
            """
            Elevation gain: 200
            Distance, time, pace (km/h), avgHR, maxHR, avgCad, maxCad, avgPower, maxPower
            10km 30:00 @20.0 150 170 82 95 160 280
            """;
        text = text.ReplaceLineEndings("\n");

        var result = new FixedTextFormatter().Format(CreateSummary(Sport.Bike, split));

        Assert.Equal(text, result);
    }

    [Fact]
    public void Format_BikeWithoutPowerMeter_ShowsDashesForPower()
    {
        var split = new Split(new TimeSpan(0, 30, 0), 10000, 150, 170,
            AvgCadence: 82, MaxCadence: 95);

        var text =
            """
            Elevation gain: 200
            Distance, time, pace (km/h), avgHR, maxHR, avgCad, maxCad, avgPower, maxPower
            10km 30:00 @20.0 150 170 82 95 - -
            """;
        text = text.ReplaceLineEndings("\n");

        var result = new FixedTextFormatter().Format(CreateSummary(Sport.Bike, split));

        Assert.Equal(text, result);
    }

    [Fact]
    public void Format_RunWithCadenceAndPower_DoesNotShowThem()
    {
        var split = new Split(new TimeSpan(0, 5, 0), 1000, 150, 170,
            AvgCadence: 85, MaxCadence: 92, AvgPower: 250, MaxPower: 400);

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            1km 05:00 @5:00 150 170
            """;
        text = text.ReplaceLineEndings("\n");

        var result = new FixedTextFormatter().Format(CreateSummary(Sport.Run, split));

        Assert.Equal(text, result);
    }

    private static ActivitySummary CreateSummary(Sport sport, Split split)
    {
        return new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: sport,
            TotalTime: split.Time,
            DistanceInMeters: split.DistanceInMeters,
            ElevationGain: sport == Sport.Bike ? 200 : null,
            Splits: [split]
        );
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
    
    [Fact]
    public void Format_MissingHeartRate_ShowsDash()
    {
        var split1 = new Split(new TimeSpan(0, 20, 00), 1700, null, 178);

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
            1.7km 20:00 @11:46 - 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_MoreThanOneHourDuration()
    {
        var split1 = new Split(new TimeSpan(2, 20, 00), 17000, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(2, 20, 0), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            17km 02:20:00 @8:14 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
    
    [Fact]
    public void Format_PaceRoundingUpToSixtySeconds_CarriesToNextMinute()
    {
        var split1 = new Split(new TimeSpan(0, 12, 35), 2100, 160, 178);

        var summary = new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 12, 35), 
            DistanceInMeters: split1.DistanceInMeters,
            ElevationGain: null,
            Splits: [split1]
        );

        var text =
            """
            Distance, time, pace (min/km), avgHR, maxHR
            2.1km 12:35 @6:00 160 178
            """;
        text = text.ReplaceLineEndings("\n");

        var formatter = new FixedTextFormatter();
        
        var result = formatter.Format(summary);
        
        Assert.Equal(text, result);
    }
}