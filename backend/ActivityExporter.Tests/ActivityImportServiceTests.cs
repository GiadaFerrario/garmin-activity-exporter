using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;

namespace ActivityExporter.Tests;

class FakeActivityParser : IActivityParser
{
    private readonly Func<Stream, ActivitySummary> _behavior;

    public FakeActivityParser(Func<Stream, ActivitySummary> behavior)
    {
        _behavior = behavior;
    }

    public ActivitySummary Parse(Stream content) => _behavior(content);
}

public class ActivityImportServiceTests
{
    private static ActivitySummary CreateSummary()
    {
        var split = new Split(new TimeSpan(0, 5, 00), 1000, 160, 178);

        return new ActivitySummary(
            Id: 1,
            StartTime: new DateTimeOffset(2026, 10, 10, 10, 10, 10, TimeSpan.Zero),
            Sport: Sport.Run,
            TotalTime: new TimeSpan(0, 8, 0),
            DistanceInMeters: split.DistanceInMeters,
            ElevationGain: null,
            Splits: [split]
        );
    }

    private static UploadedFile CreateFile(string fileName)
    {
        return new UploadedFile(fileName, new MemoryStream(new byte[100]));
    }

    [Fact]
    public void Import_ValidFitFile_ReturnsSuccess()
    {
        var parser = new FakeActivityParser(_ => CreateSummary());
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { CreateFile("run.fit") };

        var results = service.Import(files);

        var result = Assert.Single(results);
        Assert.Equal(UploadStatus.Success , result.Status);
    }

    [Fact]
    public void Import_InvalidFitFile_ReturnsError()
    {
        var parser = new FakeActivityParser(_ => CreateSummary());
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { CreateFile("run.fot") };

        var results = service.Import(files);

        var result = Assert.Single(results);
        Assert.Equal(UploadStatus.Error , result.Status);
    }

    [Fact]
    public void Import_TwoValidFitFiles_ReturnsSuccess()
    {
        var parser = new FakeActivityParser(_ => CreateSummary());
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { CreateFile("run.fit"), CreateFile("swim.fit") };

        var results = service.Import(files);

        Assert.Equal(
            new[] { UploadStatus.Success, UploadStatus.Success },
            results.Select(result => result.Status));
    }

    [Fact]
    public void Import_TwoFiles_ReturnsSuccessAndError()
    {
        var parser = new FakeActivityParser(_ => CreateSummary());
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { CreateFile("run.fit"), CreateFile("swim.f0t") };

        var results = service.Import(files);

        Assert.Equal(
            new[] { UploadStatus.Success, UploadStatus.Error },
            results.Select(result => result.Status));
    }

    [Fact]
    public void Import_ParserThrowsInvalidFileException_ReturnsErrorWithParserMessage()
    {
        var parser = new FakeActivityParser(_ => throw new InvalidFileException("Sport not supported"));
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { CreateFile("walk.fit") };

        var results = service.Import(files);

        var result = Assert.Single(results);
        Assert.Equal(UploadStatus.Error, result.Status);
        Assert.Equal("Sport not supported", result.Message);
    }
    
    [Fact]
    public void Import_EmptyFile_ReturnsError()
    {
        var parser = new FakeActivityParser(_ => throw new InvalidFileException("Uploaded file is empty"));
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { new UploadedFile("run.fit", new MemoryStream(new byte[100])) };

        var results = service.Import(files);

        var result = Assert.Single(results);
        Assert.Equal(UploadStatus.Error , result.Status);
        Assert.Equal("Uploaded file is empty", result.Message);
    }
    
    [Fact]
    public void Import_HugeFile_ReturnsError()
    {
        var parser = new FakeActivityParser(_ => throw new InvalidFileException("Uploaded file is too large"));
        var service = new ActivityImportService(parser);
        var files = new List<UploadedFile> { new UploadedFile("run.fit", new MemoryStream(new byte[10 * 1024 * 1024 + 1])) };

        var results = service.Import(files);

        var result = Assert.Single(results);
        Assert.Equal(UploadStatus.Error , result.Status);
        Assert.Equal("Uploaded file is too large", result.Message);
    }
}
