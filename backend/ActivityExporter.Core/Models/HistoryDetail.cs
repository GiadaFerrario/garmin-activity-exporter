namespace ActivityExporter.Core.Models;

/// <summary>
/// An activity of the history with its coach text
/// </summary>
/// <param name="Text">Null for the sports that have no coach text (HIIT, strength)</param>
public record HistoryDetail(SavedActivity Activity, string? Text);
