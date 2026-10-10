using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using CoreSport = ActivityExporter.Core.Models.Sport;
using Dynastream.Fit;
using FitDateTime = Dynastream.Fit.DateTime;   // alias
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
    public void Map_UnsupportedSport_ThrowsInvalidFileExceptionNamingTheSport()
    {
        var session = CreateSession(FitSport.Walking);

        var exception = Assert.Throws<InvalidFileException>(() => FitActivityMapper.Map(session, []));

        Assert.Contains("Walking", exception.Message);
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
}