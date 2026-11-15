namespace ActivityExporter.Core.Models;

/// <param name="LookbackDays">How far back a sync looks for activities (also the first one)</param>
/// <param name="MaxDownloadsPerSync">
/// How many .fit files a sync downloads at most: the limits of Garmin are not published,
/// the activities left over are done by the next sync. HIIT and strength do not count: they are not downloaded.
/// </param>
public record GarminSyncOptions(int LookbackDays = 30, int MaxDownloadsPerSync = 20);
