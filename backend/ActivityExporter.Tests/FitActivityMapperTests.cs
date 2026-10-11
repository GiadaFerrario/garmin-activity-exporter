using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using CoreSport = ActivityExporter.Core.Models.Sport;
using Dynastream.Fit;
using FitDateTime = Dynastream.Fit.DateTime; // alias
using FitSport = Dynastream.Fit.Sport;

namespace ActivityExporter.Tests;

public class FitActivityMapperTests
{
    [Fact]
    public void Map_RunningSessionWithOneActiveLap_ReturnsOneSplit()
    {
        var session = new SessionMesg();
        session.SetSport(FitSport.Running);
        session.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc)));
        session.SetTotalTimerTime(74.086f);
        session.SetTotalDistance(200f);

        var lap = new LapMesg();
        lap.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc)));
        lap.SetTotalTimerTime(74.086f);
        lap.SetTotalDistance(200f);
        lap.SetAvgHeartRate(161);
        lap.SetMaxHeartRate(177);
        lap.SetIntensity(Intensity.Active);

        var result = FitActivityMapper.Map(session, [lap]);

        Assert.Equal(CoreSport.Run, result.Sport);
        var split = Assert.Single(result.Splits);
        Assert.Equal(200.00, split.DistanceInMeters, precision: 2);
        Assert.Equal(74.086, split.Time.TotalSeconds, precision: 3);
    }

    [Fact]
    public void Map_RunningSessionWithOneActiveLapAndOneWarmUpAndOneRest_ReturnsOneSplit()
    {
        var session = CreateSession(FitSport.Running);

        var activeLap = CreateLap(Intensity.Active, 100, timer: 74.086f);
        var warmUpLap = CreateLap(Intensity.Warmup, 50, timer: 74.086f);
        var restLap = CreateLap(Intensity.Rest, 0, timer: 74.086f);

        var result = FitActivityMapper.Map(session, [activeLap, warmUpLap, restLap]);

        Assert.Single(result.Splits);
        Assert.Equal(100, result.Splits[0].DistanceInMeters);
    }

    [Fact]
    public void Map_RunningSessionMixedLaps_ReturnsTwoSplitInTheSameOrder()
    {
        var session = CreateSession(FitSport.Running);

        var warmUpLap = CreateLap(Intensity.Warmup, 50, timer: 74.086f);
        var activeLap = CreateLap(Intensity.Active, 100, timer: 74.086f);
        var restLap = CreateLap(Intensity.Rest, 0, timer: 74.086f);
        var activeLap2 = CreateLap(Intensity.Active, 200, timer: 74.086f);

        var result = FitActivityMapper.Map(session, [warmUpLap, activeLap, restLap, activeLap2]);

        Assert.Equal(2, result.Splits.Count);
        Assert.Equal(100, result.Splits[0].DistanceInMeters);
        Assert.Equal(200, result.Splits[1].DistanceInMeters);
    }

    [Fact]
    public void Map_ActiveLapAndNoiseLap_ReturnsOneSplit()
    {
        var session = CreateSession(FitSport.Running);

        var activeLap = CreateLap(Intensity.Active, 100, timer: 74.086f);
        var activeLap2 = CreateLap(Intensity.Active, 50, timer: 2.5f);
        var activeLap3 = CreateLap(Intensity.Active, 50);


        var result = FitActivityMapper.Map(session, [activeLap, activeLap2, activeLap3]);

        Assert.Single(result.Splits);
        Assert.Equal(100, result.Splits[0].DistanceInMeters);
    }

    [Theory]
    [InlineData(FitSport.Running, CoreSport.Run)]
    [InlineData(FitSport.Cycling, CoreSport.Bike)]
    [InlineData(FitSport.Swimming, CoreSport.Swim)]
    public void Map_SupportedSport_ReturnsMatchingSport(FitSport fitSport, CoreSport expected)
    {
        var session = CreateSession(fitSport);

        var result = FitActivityMapper.Map(session, []);

        Assert.Equal(expected, result.Sport);
    }

    [Fact]
    public void Map_LapWithoutIntensityAndLapWithoutDistance_KeepsOnlyTheFirst()
    {
        var session = CreateSession(FitSport.Swimming);

        var lapNoIntensity = CreateLap(distance: 200f, timer: 74.086f);
        var lapNoDistance = CreateLap();
        var lapZeroDistance = CreateLap(distance: 0f, timer: 74.086f);

        var result = FitActivityMapper.Map(session, [lapNoIntensity, lapNoDistance, lapZeroDistance]);

        Assert.Single(result.Splits);
        Assert.Equal(200f, result.Splits[0].DistanceInMeters);
    }

    [Fact]
    public void Map_UnsupportedSport_ThrowsInvalidFileExceptionNamingTheSport()
    {
        var session = CreateSession(FitSport.Walking);

        var exception = Assert.Throws<InvalidFileException>(() => FitActivityMapper.Map(session, []));

        Assert.Contains("Walking", exception.Message);
    }

    [Fact]
    public void Map_MissingSport_ThrowsInvalidFileException()
    {
        var session = new SessionMesg();
        session.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc)));
        session.SetTotalTimerTime(60f);
        session.SetTotalDistance(1000f);

        var exception = Assert.Throws<InvalidFileException>(() => FitActivityMapper.Map(session, []));

        Assert.Contains("The uploaded sport", exception.Message);
    }

    [Theory]
    [InlineData(2.9f, 0)]
    [InlineData(3f, 0)]   // the minimum duration itself is dropped: a lap must last MORE than the threshold
    [InlineData(3.1f, 1)]
    public void Map_LapAroundMinimumDuration_KeepsOnlyLapsLongerThanTheThreshold(float timer, int expectedSplits)
    {
        var session = CreateSession(FitSport.Running);
        var lap = CreateLap(Intensity.Active, 100, timer: timer);

        var result = FitActivityMapper.Map(session, [lap]);

        Assert.Equal(expectedSplits, result.Splits.Count);
    }

    [Fact]
    public void Map_SessionStartTime_ReturnsStartTimeInUtc()
    {
        var session = CreateSession(FitSport.Running);

        var result = FitActivityMapper.Map(session, []);

        Assert.Equal(TimeSpan.Zero, result.StartTime.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 9, 16, 1, 58, TimeSpan.Zero), result.StartTime);
    }

    [Fact]
    public void Map_SessionWithTimerAndElapsedTime_UsesTimerTime()
    {
        var session = CreateSession(FitSport.Cycling);
        session.SetTotalTimerTime(4774.569f);
        session.SetTotalElapsedTime(5416.564f);

        var result = FitActivityMapper.Map(session, []);

        Assert.Equal(4774.569, result.TotalTime.TotalSeconds, precision: 2);
    }

    [Fact]
    public void Map_SessionWithoutAscent_ReturnsNullElevationGain()
    {
        var session = CreateSession(FitSport.Swimming);

        var result = FitActivityMapper.Map(session, []);

        Assert.Null(result.ElevationGain);
    }

    [Fact]
    public void Map_SessionWithAscent_ReturnsElevationGain()
    {
        var session = CreateSession(FitSport.Cycling);
        session.SetTotalAscent(230);

        var result = FitActivityMapper.Map(session, []);

        Assert.Equal(230, result.ElevationGain);
    }

    [Fact]
    public void Map_LapWithoutHeartRate_ReturnsSplitWithNullHeartRates()
    {
        var session = CreateSession(FitSport.Running);
        var lapWithoutHeartRate = new LapMesg();
        lapWithoutHeartRate.SetTotalTimerTime(60f);
        lapWithoutHeartRate.SetTotalDistance(200f);

        var result = FitActivityMapper.Map(session, [lapWithoutHeartRate]);

        var split = Assert.Single(result.Splits);
        Assert.Null(split.AvgHR);
        Assert.Null(split.MaxHR);
    }

    [Fact]
    public void Map_SessionWithoutStartTime_ThrowsInvalidFileException()
    {
        var session = new SessionMesg();
        session.SetSport(FitSport.Running);
        session.SetTotalTimerTime(60f);
        session.SetTotalDistance(1000f);

        var exception = Assert.Throws<InvalidFileException>(() => FitActivityMapper.Map(session, []));

        Assert.Contains("start time", exception.Message);
    }

    private static SessionMesg CreateSession(FitSport sport)
    {
        var session = new SessionMesg();
        session.SetSport(sport);
        session.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc)));
        session.SetTotalTimerTime(60f);
        session.SetTotalDistance(1000f);
        return session;
    }

    // A null intensity or distance means "field absent from the file" (the setter is not called)
    private static LapMesg CreateLap(Intensity? intensity = null, float? distance = null, float? timer = null)
    {
        var lap = new LapMesg();
        lap.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc)));
        if (timer.HasValue)
        {
            lap.SetTotalTimerTime(timer.Value);
        }

        lap.SetAvgHeartRate(161);
        lap.SetMaxHeartRate(177);
        if (distance.HasValue)
        {
            lap.SetTotalDistance(distance.Value);
        }

        if (intensity.HasValue)
        {
            lap.SetIntensity(intensity.Value);
        }

        return lap;
    }
}