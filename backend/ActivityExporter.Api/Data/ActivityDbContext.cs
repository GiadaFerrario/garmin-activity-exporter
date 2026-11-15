using Microsoft.EntityFrameworkCore;

namespace ActivityExporter.Api;

public class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> options) : base(options)
    {
    }

    public DbSet<ActivityEntity> Activities => Set<ActivityEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEntity>(activity =>
        {
            // The only protection against a double save, even if two syncs run together.
            // A deleted activity keeps its row, so its Garmin id stays unique too.
            activity.HasIndex(a => a.GarminActivityId).IsUnique();
            activity.HasIndex(a => a.StartTimeUtc);

            // Written as text ("Run") so reordering the enum never changes the saved data
            activity.Property(a => a.Sport).HasConversion<string>();

            // The splits are always read and written together with the activity: one JSON column
            activity.OwnsMany(a => a.Splits, splits => splits.ToJson());

            // Deleted activities disappear from every query, unless it says IgnoreQueryFilters()
            activity.HasQueryFilter(a => a.DeletedAtUtc == null);
        });
    }
}
