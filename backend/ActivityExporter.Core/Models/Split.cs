namespace ActivityExporter.Core.Models;

public record Split(TimeSpan Time, double DistanceInMeters, int? AvgHR,int? MaxHR);