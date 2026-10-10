namespace ActivityExporter.Core;

public record ActivitySummary(int Id, DateTimeOffset StartTime, Sport Sport, TimeSpan TotalTime, double DistanceInMeters, int? ElevationGain, IReadOnlyList<Split> Splits);