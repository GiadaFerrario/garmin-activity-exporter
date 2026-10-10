namespace ActivityExporter.Core.Models;

public record UploadResult(string FileName, UploadStatus Status, string Message, ActivitySummary? Summary = null);