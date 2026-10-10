namespace ActivityExporter.Core;

public record UploadResult(string FileName, UploadStatus Status, string Message);