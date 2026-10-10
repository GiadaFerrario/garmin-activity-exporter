using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;

namespace ActivityExporter.Core.Service;

public class ActivityImportService
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private readonly IActivityParser _activityParser;

    public ActivityImportService(IActivityParser parser)
    {
        _activityParser = parser;
    }

    public IReadOnlyList<UploadResult> Import(IReadOnlyList<UploadedFile> files)
    {
        return files.Select(ImportFile).ToList();
    }

    private UploadResult ImportFile(UploadedFile file)
    {
        var validationError = GetValidationError(file);
        if (validationError != null)
        {
            return new UploadResult(file.FileName, UploadStatus.Error, validationError);
        }

        try
        {
            _activityParser.Parse(file.Content);
            return new UploadResult(file.FileName, UploadStatus.Success, "The file is a valid fit format");
        }
        catch (InvalidFileException ex)
        {
            return new UploadResult(file.FileName, UploadStatus.Error, ex.Message);
        }
    }

    // Cheap checks done before parsing; returns the error message, or null if the file can be parsed
    private static string? GetValidationError(UploadedFile file)
    {
        if (!file.FileName.EndsWith(".fit", StringComparison.OrdinalIgnoreCase))
        {
            return "Uploaded file is not a .fit file";
        }

        if (file.Content.Length == 0)
        {
            return "Uploaded file is empty";
        }

        if (file.Content.Length > MaxFileSizeInBytes)
        {
            return "Uploaded file is too large";
        }

        return null;
    }
}
