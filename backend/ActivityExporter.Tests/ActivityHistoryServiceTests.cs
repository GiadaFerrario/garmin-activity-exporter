using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;

namespace ActivityExporter.Tests;

class FakeActivityFormatter : IActivityFormatter
{
    public ActivitySummary? Received { get; private set; }
    public int Calls { get; private set; }

    public string Format(ActivitySummary activitySummary)
    {
        Calls++;
        Received = activitySummary;
        return "THE TEXT";
    }
}

public class ActivityHistoryServiceTests
{
    private static SavedActivity CreateActivity(Sport sport, double? distance = 4864.75, int? elevationGain = 120)
    {
        var hasSplits = sport.HasCoachText();
        return new SavedActivity(
            Id: 0,
            GarminActivityId: 1001,
            Name: "An activity",
            StartTime: new DateTimeOffset(2026, 11, 12, 10, 45, 57, TimeSpan.Zero),
            Sport: sport,
            TotalTime: new TimeSpan(0, 39, 11),
            DistanceInMeters: distance,
            ElevationGain: elevationGain,
            AvgHR: 162,
            MaxHR: 178,
            Calories: 410,
            Splits: hasSplits ? [new Split(new TimeSpan(0, 7, 17), 1000, 170, 180)] : []);
    }

    private static async Task<(ActivityHistoryService Service, FakeActivityFormatter Formatter, SavedActivity Saved)> Create(
        SavedActivity activity)
    {
        var repository = new FakeActivityRepository();
        var formatter = new FakeActivityFormatter();
        var saved = await repository.AddAsync(activity);
        return (new ActivityHistoryService(repository, formatter), formatter, saved);
    }

    [Fact]
    public async Task GetDetailAsync_RunActivity_ReturnsItWithTheFormattedText()
    {
        var (service, formatter, saved) = await Create(CreateActivity(Sport.Run));

        var detail = await service.GetDetailAsync(saved.Id);

        Assert.NotNull(detail);
        Assert.Equal(saved, detail.Activity);
        Assert.Equal("THE TEXT", detail.Text);
        // what the formatter was given is the activity, rebuilt as the summary it knows
        var summary = formatter.Received;
        Assert.NotNull(summary);
        Assert.Equal(saved.Id, summary.Id);
        Assert.Equal(saved.StartTime, summary.StartTime);
        Assert.Equal(Sport.Run, summary.Sport);
        Assert.Equal(saved.TotalTime, summary.TotalTime);
        Assert.Equal(4864.75, summary.DistanceInMeters);
        Assert.Equal(120, summary.ElevationGain);
        Assert.Equal(saved.Splits, summary.Splits);
    }

    [Theory]
    [InlineData(Sport.Hiit)]
    [InlineData(Sport.Strength)]
    public async Task GetDetailAsync_SportWithoutCoachText_HasNoTextAndTheFormatterIsNotCalled(Sport sport)
    {
        var (service, formatter, saved) = await Create(CreateActivity(sport, distance: null, elevationGain: null));

        var detail = await service.GetDetailAsync(saved.Id);

        Assert.NotNull(detail);
        Assert.Equal(saved, detail.Activity);
        Assert.Null(detail.Text);
        Assert.Equal(0, formatter.Calls);
    }

    [Fact]
    public async Task GetDetailAsync_UnknownId_ReturnsNull()
    {
        var (service, formatter, _) = await Create(CreateActivity(Sport.Run));

        Assert.Null(await service.GetDetailAsync(999));
        Assert.Equal(0, formatter.Calls);
    }

    [Fact]
    public async Task GetDetailAsync_RunActivityWithoutDistance_IsFormattedWithZeroDistance()
    {
        var (service, formatter, saved) = await Create(CreateActivity(Sport.Run, distance: null));

        await service.GetDetailAsync(saved.Id);

        Assert.Equal(0, formatter.Received!.DistanceInMeters);
    }
}
