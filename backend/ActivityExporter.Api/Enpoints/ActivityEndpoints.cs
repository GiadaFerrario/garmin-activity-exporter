using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;

namespace ActivityExporter.Api;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities");
        group.MapPost("/", Upload).DisableAntiforgery();
    }

    private static List<UploadResponseItem> Upload(
        IFormFileCollection files,
        ActivityImportService service,
        IActivityFormatter formatter)
    {
        var uploadedFiles = files
        .Select(file => new UploadedFile(file.FileName,file.OpenReadStream()))
        .ToList();

        try
        {
            var results = service.Import(uploadedFiles);

            return results.Select(result => new UploadResponseItem(
                result.FileName,
                result.Status,
                result.Message,
                Text: result.Summary is not null ? formatter.Format(result.Summary) : null
            )).ToList();
        }
        finally
        {
            // close every stream that was opened
            foreach (var file in uploadedFiles)
            {
                file.Content.Dispose();
            }
        }
    }
}