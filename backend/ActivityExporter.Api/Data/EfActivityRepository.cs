using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ActivityExporter.Api;

public class EfActivityRepository : IActivityRepository
{
    // SQLITE_CONSTRAINT_UNIQUE: the extended error code of a unique index that is violated
    private const int SqliteConstraintUnique = 2067;

    private readonly ActivityDbContext _context;

    public EfActivityRepository(ActivityDbContext context)
    {
        _context = context;
    }

    public async Task<SavedActivity> AddAsync(SavedActivity savedActivity)
    {
        var entity = ToEntity(savedActivity);
        _context.Activities.Add(entity);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // The database refused it. Forget the entity, otherwise the context would try to
            // save it again at the next SaveChanges and fail again
            _context.Entry(entity).State = EntityState.Detached;
            throw new DuplicateActivityException(savedActivity.GarminActivityId, exception);
        }

        return ToModel(entity);
    }

    public async Task<IReadOnlyList<SavedActivity>> ListAsync()
    {
        // AsNoTracking: these objects are only read, the change tracker does not need to follow them
        var entities = await _context.Activities
            .AsNoTracking()
            .OrderByDescending(activity => activity.StartTimeUtc)
            .ToListAsync();

        return entities.Select(ToModel).ToList();
    }

    public async Task<SavedActivity?> GetAsync(int id)
    {
        var entity = await _context.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(activity => activity.Id == id);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlySet<long>> GetKnownGarminIdsAsync(DateTimeOffset since)
    {
        var sinceUtc = since.UtcDateTime;

        // IgnoreQueryFilters: the deleted activities count too, they must not come back at the next sync
        var ids = await _context.Activities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(activity => activity.StartTimeUtc >= sinceUtc)
            .Select(activity => activity.GarminActivityId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Tracked on purpose: the change below must be saved. The query filter already hides
        // the deleted ones, so deleting a deleted activity answers false like an unknown id
        var entity = await _context.Activities.FirstOrDefaultAsync(activity => activity.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.DeletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique };
    }

    // The Id is not copied: the database assigns it
    private static ActivityEntity ToEntity(SavedActivity activity)
    {
        return new ActivityEntity
        {
            GarminActivityId = activity.GarminActivityId,
            Name = activity.Name,
            StartTimeUtc = activity.StartTime.UtcDateTime,
            Sport = activity.Sport,
            TotalTime = activity.TotalTime,
            DistanceInMeters = activity.DistanceInMeters,
            ElevationGain = activity.ElevationGain,
            AvgHR = activity.AvgHR,
            MaxHR = activity.MaxHR,
            Calories = activity.Calories,
            Splits = activity.Splits.ToList()
        };
    }

    private static SavedActivity ToModel(ActivityEntity entity)
    {
        // SQLite gives the date back without a "this is UTC" mark: say it
        var startTime = new DateTimeOffset(DateTime.SpecifyKind(entity.StartTimeUtc, DateTimeKind.Utc));

        return new SavedActivity(
            entity.Id,
            entity.GarminActivityId,
            entity.Name,
            startTime,
            entity.Sport,
            entity.TotalTime,
            entity.DistanceInMeters,
            entity.ElevationGain,
            entity.AvgHR,
            entity.MaxHR,
            entity.Calories,
            entity.Splits);
    }
}
