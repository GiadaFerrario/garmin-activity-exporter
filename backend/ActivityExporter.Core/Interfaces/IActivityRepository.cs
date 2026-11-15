using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Interfaces;

public interface IActivityRepository
{
    /// <summary>
    /// Saves a new activity in the history.
    /// Fails if its GarminActivityId is already known, whether that activity is saved or deleted.
    /// </summary>
    /// <param name="savedActivity">The activity to save; its Id is ignored, the repository assigns it</param>
    /// <returns>The saved activity, with the Id assigned by the repository</returns>
    /// <exception cref="Exception">When the GarminActivityId is already known (a dedicated exception will replace this one)</exception>
    Task<SavedActivity> AddAsync(SavedActivity savedActivity);

    /// <summary>
    /// Lists the activities of the history, the most recent first. Deleted activities are not included.
    /// </summary>
    /// <returns>All the activities that are not deleted, ordered by start time, newest first</returns>
    Task<IReadOnlyList<SavedActivity>> ListAsync();

    /// <summary>
    /// Gets one activity of the history, with its splits.
    /// </summary>
    /// <param name="id">The Id of the repository (not the GarminActivityId)</param>
    /// <returns>The activity, or null if the Id does not exist or the activity is deleted</returns>
    Task<SavedActivity?> GetAsync(int id);

    /// <summary>
    /// Gets the Garmin ids the history already knows, so that a sync does not fetch them again.
    /// Deleted activities are included on purpose: a deleted activity must not come back at the next sync.
    /// </summary>
    /// <param name="since">Only the activities that started at this moment or later are considered</param>
    /// <returns>The GarminActivityId of every known activity (saved or deleted) that started since the given moment</returns>
    Task<IReadOnlySet<long>> GetKnownGarminIdsAsync(DateTimeOffset since);

    /// <summary>
    /// Deletes an activity from the history without losing its GarminActivityId (soft delete):
    /// the activity disappears from ListAsync and GetAsync but the id stays known.
    /// </summary>
    /// <param name="id">The Id of the repository (not the GarminActivityId)</param>
    /// <returns>True if the activity was deleted, false if the Id does not exist</returns>
    Task<bool> DeleteAsync(int id);
}