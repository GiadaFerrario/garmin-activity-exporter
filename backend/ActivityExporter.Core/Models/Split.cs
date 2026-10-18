namespace ActivityExporter.Core.Models;

public record Split(TimeSpan Time, double DistanceInMeters, int? AvgHR,int? MaxHR, int? AvgCadence = null,  int? MaxCadence = null, int? AvgPower = null, int? MaxPower = null);