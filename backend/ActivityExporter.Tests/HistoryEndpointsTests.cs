using ActivityExporter.Api;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ActivityExporter.Tests;

public class HistoryEndpointsTests
{
    private static readonly DateTimeOffset Older = new(2026, 11, 10, 6, 42, 34, TimeSpan.Zero);
    private static readonly DateTimeOffset Newer = new(2026, 11, 12, 10, 45, 57, TimeSpan.Zero);

    private static SavedActivity Run(long garminId, DateTimeOffset start) => new(
        Id: 0, garminId, "Morning run", start, Sport.Run, new TimeSpan(0, 39, 11), 4864.75, 120, 162, 178, 410,
        Splits: [new Split(new TimeSpan(0, 7, 17), 1000, 170, 180)]);

    private static SavedActivity Hiit(long garminId, DateTimeOffset start) => new(
        Id: 0, garminId, "HIIT", start, Sport.Hiit, new TimeSpan(0, 43, 43), null, null, 139, 178, 242, Splits: []);

    private static async Task<(FakeActivityRepository Repository, ActivityHistoryService History)> Create(
        params SavedActivity[] activities)
    {
        var repository = new FakeActivityRepository();
        foreach (var activity in activities)
        {
            await repository.AddAsync(activity);
        }

        return (repository, new ActivityHistoryService(repository, new FakeActivityFormatter()));
    }

    [Fact]
    public async Task List_ReturnsTheActivitiesNewestFirstWithoutText()
    {
        var (repository, _) = await Create(Hiit(2001, Older), Run(1001, Newer));

        var result = await HistoryEndpoints.List(repository);

        var items = result.Value!;
        Assert.Equal([1001L, 2001L], items.Select(item => item.GarminActivityId));

        var run = items[0];
        Assert.Equal(2, run.Id);
        Assert.Equal("Morning run", run.Name);
        Assert.Equal(Sport.Run, run.Sport);
        Assert.Equal(Newer, run.StartTime);
        Assert.Equal(new TimeSpan(0, 39, 11).TotalSeconds, run.DurationInSeconds);
        Assert.Equal(4864.75, run.DistanceInMeters);
        Assert.Equal(120, run.ElevationGain);
        Assert.Equal(162, run.AvgHR);
        Assert.Equal(178, run.MaxHR);
        Assert.Equal(410, run.Calories);
        Assert.True(run.HasCoachText);
        Assert.Null(run.Text);

        var hiit = items[1];
        Assert.Equal(Sport.Hiit, hiit.Sport);
        Assert.Null(hiit.DistanceInMeters);
        Assert.False(hiit.HasCoachText);
        Assert.Null(hiit.Text);
    }

    [Fact]
    public async Task List_EmptyHistory_ReturnsAnEmptyList()
    {
        var (repository, _) = await Create();

        var result = await HistoryEndpoints.List(repository);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Get_RunActivity_ReturnsItWithTheCoachText()
    {
        var (_, history) = await Create(Run(1001, Newer));

        var result = await HistoryEndpoints.Get(1, history);

        var ok = Assert.IsType<Ok<HistoryActivityResponse>>(result.Result);
        Assert.Equal(1001, ok.Value!.GarminActivityId);
        Assert.True(ok.Value.HasCoachText);
        Assert.Equal("THE TEXT", ok.Value.Text);
    }

    [Fact]
    public async Task Get_HiitActivity_ReturnsItWithoutText()
    {
        var (_, history) = await Create(Hiit(2001, Older));

        var result = await HistoryEndpoints.Get(1, history);

        var ok = Assert.IsType<Ok<HistoryActivityResponse>>(result.Result);
        Assert.False(ok.Value!.HasCoachText);
        Assert.Null(ok.Value.Text);
    }

    [Fact]
    public async Task Get_UnknownId_IsNotFound()
    {
        var (_, history) = await Create(Run(1001, Newer));

        var result = await HistoryEndpoints.Get(999, history);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingActivity_IsNoContentAndTheActivityIsGone()
    {
        var (repository, _) = await Create(Run(1001, Newer));

        var result = await HistoryEndpoints.Delete(1, repository);

        Assert.IsType<NoContent>(result.Result);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task Delete_UnknownId_IsNotFound()
    {
        var (repository, _) = await Create(Run(1001, Newer));

        var result = await HistoryEndpoints.Delete(999, repository);

        Assert.IsType<NotFound>(result.Result);
        Assert.Single(repository.Saved);
    }
}
