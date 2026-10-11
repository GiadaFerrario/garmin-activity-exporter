using ActivityExporter.Core.Models;

namespace ActivityExporter.Api;

public record UploadResponseItem(string FileName, UploadStatus Status, string Message, string? Text);