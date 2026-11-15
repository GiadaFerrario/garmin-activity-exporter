using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ActivityExporter.Tests;

// These tests use a real SQLite database, in memory: unlike the InMemory provider of EF it also
// applies the unique index and translates the queries to SQL like the real database does.
public class EfActivityRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ActivityDbContext> _options;

    public EfActivityRepositoryTests()
    {
        // An in-memory SQLite database lives as long as its connection is open: one per test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ActivityDbContext>().UseSqlite(_connection).Options;

        using var context = new ActivityDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    // A new context (so a new repository) for every operation: a read never gets an object
    // that the change tracker still remembers from the write, it really goes to the database
    private EfActivityRepository NewRepository() => new(new ActivityDbContext(_options));

    private static SavedActivity CreateRun(long garminId = 1001, DateTimeOffset? start = null)
    {
        return new SavedActivity(
            Id: 0,
            GarminActivityId: garminId,
            Name: "Morning run",
            StartTime: start ?? new DateTimeOffset(2026, 11, 12, 10, 45, 57, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 39, 11),
            DistanceInMeters: 4864.75,
            ElevationGain: null,
            AvgHR: 162,
            MaxHR: 178,
            Calories: 410,
            Splits:
            [
                new Split(new TimeSpan(0, 7, 17), 1000, 170, 180),
                new Split(new TimeSpan(0, 0, 20), 60, null, 168, AvgCadence: 90, MaxPower: 250)
            ]);
    }

    private static SavedActivity CreateHiit(long garminId = 2001, DateTimeOffset? start = null)
    {
        return new SavedActivity(
            Id: 0,
            GarminActivityId: garminId,
            Name: "HIIT",
            StartTime: start ?? new DateTimeOffset(2026, 11, 10, 6, 42, 34, TimeSpan.Zero),
            Sport: Sport.Hiit,
            TotalTime: new TimeSpan(0, 43, 43),
            DistanceInMeters: null,
            ElevationGain: null,
            AvgHR: 139,
            MaxHR: 178,
            Calories: 242,
            Splits: []);
    }

    [Fact]
    public async Task AddAsync_NewActivity_ReturnsItWithAssignedId()
    {
        var saved = await NewRepository().AddAsync(CreateRun());

        Assert.True(saved.Id > 0);
        Assert.Equal(1001, saved.GarminActivityId);
    }

    [Fact]
    public async Task AddAsync_ThenGet_ReturnsTheSameActivityWithItsSplits()
    {
        var original = CreateRun();
        var saved = await NewRepository().AddAsync(original);

        var read = await NewRepository().GetAsync(saved.Id);

        Assert.NotNull(read);
        Assert.Equal(saved.Id, read.Id);
        Assert.Equal(original.GarminActivityId, read.GarminActivityId);
        Assert.Equal(original.Name, read.Name);
        Assert.Equal(original.StartTime, read.StartTime);
        Assert.Equal(original.Sport, read.Sport);
        Assert.Equal(original.TotalTime, read.TotalTime);
        Assert.Equal(original.DistanceInMeters, read.DistanceInMeters);
        Assert.Equal(original.ElevationGain, read.ElevationGain);
        Assert.Equal(original.AvgHR, read.AvgHR);
        Assert.Equal(original.MaxHR, read.MaxHR);
        Assert.Equal(original.Calories, read.Calories);
        Assert.Equal(original.Splits, read.Splits);
    }

    [Fact]
    public async Task AddAsync_ActivityWithoutDistanceAndSplits_RoundTrips()
    {
        var saved = await NewRepository().AddAsync(CreateHiit());

        var read = await NewRepository().GetAsync(saved.Id);

        Assert.NotNull(read);
        Assert.Equal(Sport.Hiit, read.Sport);
        Assert.Null(read.DistanceInMeters);
        Assert.Empty(read.Splits);
    }

    [Fact]
    public async Task AddAsync_StartTimeWithAnOffset_IsReadBackAsTheSameMoment()
    {
        // 11:45:57 at +01:00 is 10:45:57 UTC: DateTimeOffset equality compares the moment, not the clock
        var withOffset = new DateTimeOffset(2026, 11, 12, 11, 45, 57, TimeSpan.FromHours(1));
        var saved = await NewRepository().AddAsync(CreateRun(start: withOffset));

        var read = await NewRepository().GetAsync(saved.Id);

        Assert.NotNull(read);
        Assert.Equal(withOffset, read.StartTime);
    }

    [Fact]
    public async Task AddAsync_SameGarminIdTwice_ThrowsDuplicateActivityException()
    {
        await NewRepository().AddAsync(CreateRun(garminId: 1001));

        var exception = await Assert.ThrowsAsync<DuplicateActivityException>(
            () => NewRepository().AddAsync(CreateRun(garminId: 1001)));

        Assert.Equal(1001, exception.GarminActivityId);
    }

    [Fact]
    public async Task AddAsync_GarminIdOfADeletedActivity_ThrowsDuplicateActivityException()
    {
        var saved = await NewRepository().AddAsync(CreateRun(garminId: 1001));
        await NewRepository().DeleteAsync(saved.Id);

        await Assert.ThrowsAsync<DuplicateActivityException>(
            () => NewRepository().AddAsync(CreateRun(garminId: 1001)));
    }

    [Fact]
    public async Task AddAsync_AfterADuplicate_TheSameRepositoryStillWorks()
    {
        var repository = NewRepository();
        await repository.AddAsync(CreateRun(garminId: 1001));
        await Assert.ThrowsAsync<DuplicateActivityException>(() => repository.AddAsync(CreateRun(garminId: 1001)));

        var saved = await repository.AddAsync(CreateRun(garminId: 1002));

        Assert.Equal(1002, saved.GarminActivityId);
    }

    [Fact]
    public async Task ListAsync_ReturnsTheNewestFirst()
    {
        var oldest = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var middle = new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
        var newest = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        // saved in an order that is not the one of the dates
        await NewRepository().AddAsync(CreateRun(garminId: 1, start: middle));
        await NewRepository().AddAsync(CreateRun(garminId: 2, start: newest));
        await NewRepository().AddAsync(CreateRun(garminId: 3, start: oldest));

        var list = await NewRepository().ListAsync();

        Assert.Equal(new long[] { 2, 1, 3 }, list.Select(a => a.GarminActivityId));
    }

    [Fact]
    public async Task ListAsync_DoesNotIncludeDeletedActivities()
    {
        var kept = await NewRepository().AddAsync(CreateRun(garminId: 1));
        var deleted = await NewRepository().AddAsync(CreateRun(garminId: 2));
        await NewRepository().DeleteAsync(deleted.Id);

        var list = await NewRepository().ListAsync();

        var only = Assert.Single(list);
        Assert.Equal(kept.Id, only.Id);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        Assert.Null(await NewRepository().GetAsync(999));
    }

    [Fact]
    public async Task GetAsync_DeletedActivity_ReturnsNull()
    {
        var saved = await NewRepository().AddAsync(CreateRun());
        await NewRepository().DeleteAsync(saved.Id);

        Assert.Null(await NewRepository().GetAsync(saved.Id));
    }

    [Fact]
    public async Task DeleteAsync_ExistingActivity_ReturnsTrue()
    {
        var saved = await NewRepository().AddAsync(CreateRun());

        Assert.True(await NewRepository().DeleteAsync(saved.Id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        Assert.False(await NewRepository().DeleteAsync(999));
    }

    [Fact]
    public async Task GetKnownGarminIdsAsync_IncludesSavedAndDeleted_OnlyFromTheGivenMoment()
    {
        var before = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var since = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        await NewRepository().AddAsync(CreateRun(garminId: 1, start: before));
        await NewRepository().AddAsync(CreateRun(garminId: 2, start: after));
        var deleted = await NewRepository().AddAsync(CreateRun(garminId: 3, start: after.AddDays(1)));
        await NewRepository().DeleteAsync(deleted.Id);

        var known = await NewRepository().GetKnownGarminIdsAsync(since);

        Assert.Equal(new long[] { 2, 3 }, known.OrderBy(id => id));
    }
}
