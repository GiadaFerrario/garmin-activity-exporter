using ActivityExporter.Api;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ActivityExporter.Tests;

public class GarminEndpointsTests
{
    private static readonly DateTimeOffset Now = new MutableTimeProvider().Now;

    private static GarminSyncService CreateService(
        FakeGarminSource source, FakeActivityRepository repository, GarminSyncOptions? options = null)
    {
        var summary = new ActivitySummary(
            Id: 0,
            StartTime: new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 39, 11),
            DistanceInMeters: 4864.75,
            ElevationGain: null,
            Splits: [new Split(new TimeSpan(0, 7, 17), 1000, 170, 180)]);
        var importService = new ActivityImportService(new FakeActivityParser(_ => summary));
        return new GarminSyncService(source, repository, importService, new MutableTimeProvider(), options);
    }

    private static GarminActivityListItem Item(long id, string typeKey, DateTimeOffset start) =>
        new(id, $"Activity {id}", typeKey, start, TimeSpan.FromSeconds(2351), 162, 178, 410);

    [Fact]
    public async Task Sync_ReturnsTheOutcomeOfEveryNewActivity()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Item(1, "walking", Now.AddDays(-3)));
        source.Items.Add(Item(2, "running", Now.AddDays(-1)));
        source.Items.Add(Item(3, "hiit", Now.AddDays(-2)));

        var result = await GarminEndpoints.Sync(CreateService(source, repository));

        var ok = Assert.IsType<Ok<GarminSyncResponse>>(result.Result);
        var outcomes = ok.Value!.Outcomes;
        Assert.Equal([2L, 3L, 1L], outcomes.Select(outcome => outcome.GarminActivityId));

        var run = outcomes[0];
        Assert.Equal(GarminSyncStatus.Saved, run.Status);
        Assert.Equal("Activity 2", run.Name);
        Assert.Equal("running", run.TypeKey);
        Assert.Equal(Sport.Run, run.Sport);
        Assert.Equal(Now.AddDays(-1), run.StartTime);
        Assert.Equal(2351, run.DurationInSeconds);
        Assert.Null(run.Message);
        // the id of the history, to open the activity afterwards
        Assert.Equal(repository.Saved.Single(activity => activity.GarminActivityId == 2).Id, run.SavedId);

        var walking = outcomes[2];
        Assert.Equal(GarminSyncStatus.Unsupported, walking.Status);
        Assert.Null(walking.Sport);
        Assert.Null(walking.SavedId);
        Assert.Contains("walking", walking.Message);

        Assert.Equal(0, ok.Value.PostponedCount);
    }

    [Fact]
    public async Task Sync_ActivitiesLeftForTheNextSync_AreCounted()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Item(1, "running", Now.AddDays(-1)));
        source.Items.Add(Item(2, "running", Now.AddDays(-2)));
        source.Items.Add(Item(3, "running", Now.AddDays(-3)));
        var service = CreateService(source, repository, new GarminSyncOptions(MaxDownloadsPerSync: 1));

        var result = await GarminEndpoints.Sync(service);

        var ok = Assert.IsType<Ok<GarminSyncResponse>>(result.Result);
        Assert.Equal(2, ok.Value!.PostponedCount);
        Assert.Equal(2, ok.Value.Outcomes.Count(outcome => outcome.Status == GarminSyncStatus.Postponed));
    }

    [Fact]
    public async Task Sync_NothingNew_ReturnsNoOutcomes()
    {
        var result = await GarminEndpoints.Sync(CreateService(new FakeGarminSource(), new FakeActivityRepository()));

        var ok = Assert.IsType<Ok<GarminSyncResponse>>(result.Result);
        Assert.Empty(ok.Value!.Outcomes);
    }

    [Fact]
    public async Task Sync_GarminFails_IsABadGatewayWithTheMessage()
    {
        var source = new FakeGarminSource { ListFails = true };

        var result = await GarminEndpoints.Sync(CreateService(source, new FakeActivityRepository()));

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(502, problem.StatusCode);
        Assert.Equal("Garmin is not reachable", problem.ProblemDetails.Detail);
    }
}
