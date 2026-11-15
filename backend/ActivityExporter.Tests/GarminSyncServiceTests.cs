using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;

namespace ActivityExporter.Tests;

class TrackingStream : MemoryStream
{
    public bool IsDisposed { get; private set; }

    public TrackingStream() : base(new byte[100])
    {
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}

class FakeGarminSource : IGarminActivitySource
{
    public List<GarminActivityListItem> Items { get; } = [];
    public HashSet<long> FailingDownloads { get; } = [];
    public bool ListFails { get; set; }
    public List<long> Downloaded { get; } = [];
    public List<TrackingStream> Streams { get; } = [];
    public (DateTimeOffset From, DateTimeOffset To)? ListedPeriod { get; private set; }

    public Task<IReadOnlyList<GarminActivityListItem>> ListActivitiesAsync(DateTimeOffset from, DateTimeOffset to)
    {
        if (ListFails)
        {
            throw new GarminSourceException("Garmin is not reachable");
        }

        ListedPeriod = (from, to);
        return Task.FromResult<IReadOnlyList<GarminActivityListItem>>(Items);
    }

    public Task<Stream> DownloadFitAsync(long garminActivityId)
    {
        Downloaded.Add(garminActivityId);
        if (FailingDownloads.Contains(garminActivityId))
        {
            throw new GarminSourceException("Download refused");
        }

        var stream = new TrackingStream();
        Streams.Add(stream);
        return Task.FromResult<Stream>(stream);
    }
}

class FakeActivityRepository : IActivityRepository
{
    public List<SavedActivity> Saved { get; } = [];
    public HashSet<long> Known { get; } = [];
    public HashSet<long> DuplicateOnAdd { get; } = [];
    public DateTimeOffset? KnownAskedSince { get; private set; }

    public Task<SavedActivity> AddAsync(SavedActivity savedActivity)
    {
        if (DuplicateOnAdd.Contains(savedActivity.GarminActivityId))
        {
            throw new DuplicateActivityException(savedActivity.GarminActivityId);
        }

        var saved = savedActivity with { Id = Saved.Count + 1 };
        Saved.Add(saved);
        return Task.FromResult(saved);
    }

    public Task<IReadOnlySet<long>> GetKnownGarminIdsAsync(DateTimeOffset since)
    {
        KnownAskedSince = since;
        return Task.FromResult<IReadOnlySet<long>>(Known);
    }

    public Task<IReadOnlyList<SavedActivity>> ListAsync() => throw new NotImplementedException();
    public Task<SavedActivity?> GetAsync(int id) => throw new NotImplementedException();
    public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
}

public class GarminSyncServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static GarminActivityListItem Run(long id, DateTimeOffset start, string typeKey = "running")
    {
        return new GarminActivityListItem(id, $"Run {id}", typeKey, start, TimeSpan.FromSeconds(2351), 162, 178, 410);
    }

    private static GarminActivityListItem Fitness(long id, DateTimeOffset start, string typeKey)
    {
        return new GarminActivityListItem(id, $"Fitness {id}", typeKey, start, new TimeSpan(0, 43, 43), 139, 178, 242);
    }

    // What the parser "finds" in the .fit file; its start time is not the one of the list on purpose
    private static ActivitySummary CreateSummary(Sport sport)
    {
        return new ActivitySummary(
            Id: 0,
            StartTime: new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Sport: sport,
            TotalTime: new TimeSpan(0, 39, 11),
            DistanceInMeters: 4864.75,
            ElevationGain: 120,
            Splits: [new Split(new TimeSpan(0, 7, 17), 1000, 170, 180)]);
    }

    private static GarminSyncService CreateService(
        FakeGarminSource source,
        FakeActivityRepository repository,
        Func<Stream, ActivitySummary>? parse = null,
        GarminSyncOptions? options = null)
    {
        var importService = new ActivityImportService(new FakeActivityParser(parse ?? (_ => CreateSummary(Sport.Run))));
        return new GarminSyncService(source, repository, importService, new FixedTimeProvider(), options);
    }

    [Fact]
    public async Task SyncAsync_NewRunActivity_IsDownloadedParsedAndSaved()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        var item = Run(1001, Now.AddDays(-1));
        source.Items.Add(item);

        var result = await CreateService(source, repository).SyncAsync();

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(GarminSyncStatus.Saved, outcome.Status);
        Assert.Equal([1001L], source.Downloaded);
        var saved = Assert.Single(repository.Saved);
        Assert.Equal(saved, outcome.Saved);
        Assert.Equal(1001, saved.GarminActivityId);
        Assert.Equal("Run 1001", saved.Name);
        // the start time is the one of the list, the rest of the sport comes from the .fit file
        Assert.Equal(item.StartTime, saved.StartTime);
        Assert.Equal(Sport.Run, saved.Sport);
        Assert.Equal(new TimeSpan(0, 39, 11), saved.TotalTime);
        Assert.Equal(4864.75, saved.DistanceInMeters);
        Assert.Equal(120, saved.ElevationGain);
        Assert.Equal([new Split(new TimeSpan(0, 7, 17), 1000, 170, 180)], saved.Splits);
        // heart rate and calories are only in the list
        Assert.Equal(162, saved.AvgHR);
        Assert.Equal(178, saved.MaxHR);
        Assert.Equal(410, saved.Calories);
    }

    [Fact]
    public async Task SyncAsync_ActivityAlreadyInTheHistory_IsNeitherDownloadedNorSaved()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        repository.Known.Add(1001);
        source.Items.Add(Run(1001, Now.AddDays(-1)));

        var result = await CreateService(source, repository).SyncAsync();

        Assert.Empty(result.Outcomes);
        Assert.Empty(source.Downloaded);
        Assert.Empty(repository.Saved);
    }

    [Theory]
    [InlineData("hiit", Sport.Hiit)]
    [InlineData("strength_training", Sport.Strength)]
    public async Task SyncAsync_SportWithoutCoachText_IsSavedFromTheListWithoutDownload(string typeKey, Sport sport)
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        var item = Fitness(2001, Now.AddDays(-2), typeKey);
        source.Items.Add(item);

        var result = await CreateService(source, repository).SyncAsync();

        Assert.Equal(GarminSyncStatus.Saved, Assert.Single(result.Outcomes).Status);
        Assert.Empty(source.Downloaded);
        var saved = Assert.Single(repository.Saved);
        Assert.Equal(sport, saved.Sport);
        Assert.Equal(item.StartTime, saved.StartTime);
        Assert.Equal(item.Duration, saved.TotalTime);
        Assert.Null(saved.DistanceInMeters);
        Assert.Null(saved.ElevationGain);
        Assert.Empty(saved.Splits);
        Assert.Equal(139, saved.AvgHR);
        Assert.Equal(178, saved.MaxHR);
        Assert.Equal(242, saved.Calories);
    }

    [Fact]
    public async Task SyncAsync_UnsupportedType_IsReportedAndNotSaved()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Fitness(3001, Now.AddDays(-1), "walking"));

        var result = await CreateService(source, repository).SyncAsync();

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(GarminSyncStatus.Unsupported, outcome.Status);
        Assert.Null(outcome.Sport);
        Assert.Null(outcome.Saved);
        Assert.Equal("Fitness 3001", outcome.Name);
        Assert.Contains("walking", outcome.Message);
        Assert.Empty(source.Downloaded);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task SyncAsync_DownloadFails_ThatActivityIsAnErrorAndTheOthersAreStillSaved()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(1, Now.AddDays(-1)));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        source.FailingDownloads.Add(1);

        var result = await CreateService(source, repository).SyncAsync();

        var outcomes = result.Outcomes.ToDictionary(outcome => outcome.GarminActivityId);
        Assert.Equal(GarminSyncStatus.Error, outcomes[1].Status);
        Assert.Equal("Download refused", outcomes[1].Message);
        Assert.Null(outcomes[1].Saved);
        Assert.Equal(GarminSyncStatus.Saved, outcomes[2].Status);
        Assert.Equal([2L], repository.Saved.Select(activity => activity.GarminActivityId));
    }

    [Fact]
    public async Task SyncAsync_ParserRejectsTheFile_ThatActivityIsAnErrorWithTheParserMessage()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(1, Now.AddDays(-1)));
        var service = CreateService(source, repository, _ => throw new InvalidFileException("The provided fit file is corrupted"));

        var result = await service.SyncAsync();

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(GarminSyncStatus.Error, outcome.Status);
        Assert.Equal("The provided fit file is corrupted", outcome.Message);
        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task SyncAsync_MoreDownloadsThanTheLimit_TheOldestArePostponed()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(3, Now.AddDays(-3)));
        source.Items.Add(Run(1, Now.AddDays(-1)));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        var service = CreateService(source, repository, options: new GarminSyncOptions(MaxDownloadsPerSync: 2));

        var result = await service.SyncAsync();

        var outcomes = result.Outcomes.ToDictionary(outcome => outcome.GarminActivityId);
        Assert.Equal(GarminSyncStatus.Saved, outcomes[1].Status);
        Assert.Equal(GarminSyncStatus.Saved, outcomes[2].Status);
        Assert.Equal(GarminSyncStatus.Postponed, outcomes[3].Status);
        Assert.Null(outcomes[3].Saved);
        Assert.Equal(1, result.PostponedCount);
        Assert.Equal(2, source.Downloaded.Count);
        Assert.DoesNotContain(3L, source.Downloaded);
    }

    [Fact]
    public async Task SyncAsync_SportsWithoutDownloadDoNotCountForTheLimit()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Fitness(1, Now.AddDays(-1), "hiit"));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        var service = CreateService(source, repository, options: new GarminSyncOptions(MaxDownloadsPerSync: 1));

        var result = await service.SyncAsync();

        Assert.All(result.Outcomes, outcome => Assert.Equal(GarminSyncStatus.Saved, outcome.Status));
        Assert.Equal(2, repository.Saved.Count);
    }

    [Fact]
    public async Task SyncAsync_FailedDownloadsCountForTheLimit()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(1, Now.AddDays(-1)));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        source.FailingDownloads.Add(1);
        var service = CreateService(source, repository, options: new GarminSyncOptions(MaxDownloadsPerSync: 1));

        var result = await service.SyncAsync();

        var outcomes = result.Outcomes.ToDictionary(outcome => outcome.GarminActivityId);
        Assert.Equal(GarminSyncStatus.Error, outcomes[1].Status);
        Assert.Equal(GarminSyncStatus.Postponed, outcomes[2].Status);
    }

    [Fact]
    public async Task SyncAsync_ReturnsTheOutcomesNewestFirst()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(2, Now.AddDays(-5)));
        source.Items.Add(Fitness(3, Now.AddDays(-1), "walking"));
        source.Items.Add(Run(1, Now.AddDays(-9)));
        source.Items.Add(Fitness(4, Now.AddDays(-3), "hiit"));

        var result = await CreateService(source, repository).SyncAsync();

        Assert.Equal([3L, 4L, 2L, 1L], result.Outcomes.Select(outcome => outcome.GarminActivityId));
    }

    [Fact]
    public async Task SyncAsync_ByDefault_LooksBackThirtyDays()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();

        await CreateService(source, repository).SyncAsync();

        Assert.Equal((Now.AddDays(-30), Now), source.ListedPeriod);
        Assert.Equal(Now.AddDays(-30), repository.KnownAskedSince);
    }

    [Fact]
    public async Task SyncAsync_WithLookbackOption_UsesThatPeriod()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        var service = CreateService(source, repository, options: new GarminSyncOptions(LookbackDays: 7));

        await service.SyncAsync();

        Assert.Equal((Now.AddDays(-7), Now), source.ListedPeriod);
        Assert.Equal(Now.AddDays(-7), repository.KnownAskedSince);
    }

    [Fact]
    public async Task SyncAsync_GarminCannotListTheActivities_FailsWithGarminSourceException()
    {
        var source = new FakeGarminSource { ListFails = true };
        var repository = new FakeActivityRepository();

        await Assert.ThrowsAsync<GarminSourceException>(() => CreateService(source, repository).SyncAsync());

        Assert.Empty(repository.Saved);
    }

    [Fact]
    public async Task SyncAsync_ActivityBecameKnownMeanwhile_IsSkippedAndTheOthersAreSaved()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(1, Now.AddDays(-1)));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        // for example another sync saved it a moment ago: the repository refuses it as a duplicate
        repository.DuplicateOnAdd.Add(1);

        var result = await CreateService(source, repository).SyncAsync();

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(2, outcome.GarminActivityId);
        Assert.Equal([2L], repository.Saved.Select(activity => activity.GarminActivityId));
    }

    [Fact]
    public async Task SyncAsync_ClosesEveryDownloadedStream()
    {
        var source = new FakeGarminSource();
        var repository = new FakeActivityRepository();
        source.Items.Add(Run(1, Now.AddDays(-1)));
        source.Items.Add(Run(2, Now.AddDays(-2)));
        var service = CreateService(source, repository, _ => throw new InvalidFileException("corrupted"));

        await service.SyncAsync();

        Assert.Equal(2, source.Streams.Count);
        Assert.All(source.Streams, stream => Assert.True(stream.IsDisposed));
    }
}
