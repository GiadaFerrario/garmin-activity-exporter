namespace ActivityExporter.Core;

public record Split(TimeSpan Time, double DistanceInMeters,int? AvgHR,int? MaxHR);