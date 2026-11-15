namespace ActivityExporter.Core.Models;

public enum GarminSyncStatus
{
    /// <summary>The activity is now in the history</summary>
    Saved,

    /// <summary>The sport is not supported: it is only shown, not saved</summary>
    Unsupported,

    /// <summary>Not downloaded in this sync because of the limit of downloads: a new sync will do it</summary>
    Postponed,

    /// <summary>The activity could not be saved: see the message</summary>
    Error
}
