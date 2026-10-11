using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using Dynastream.Fit;
using FitDateTime = Dynastream.Fit.DateTime;
using FitSport = Dynastream.Fit.Sport;
using Sport = ActivityExporter.Core.Models.Sport;

namespace ActivityExporter.Tests;

public class FitActivityParserTests
{
    [Fact]
    public void Parse_NonFitBytes_ThrowsInvalidFileExceptionWrappingFitException()
    {
        var parser = new FitActivityParser();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        var exception = Assert.Throws<InvalidFileException>(() => parser.Parse(stream));

        Assert.IsType<FitException>(exception.InnerException);
        Assert.Equal("The provided fit file is not a valid fit file", exception.Message);
    }

    [Fact]
    public void Parse_EmptyStream_ThrowsInvalidFileException()
    {
        var parser = new FitActivityParser();
        var stream = new MemoryStream();

        Assert.Throws<InvalidFileException>(() => parser.Parse(stream));
    }

    [Fact]
    public void Parse_TruncatedFile_ThrowsInvalidFileExceptionWrappingFitException()
    {
        var parser = new FitActivityParser();
        var validBytes = CreateFitBytes(FitSport.Running);
        var stream = new MemoryStream(validBytes[..^10]);

        var exception = Assert.Throws<InvalidFileException>(() => parser.Parse(stream));

        Assert.IsType<FitException>(exception.InnerException);
    }

    [Fact]
    public void Parse_FileWithCorruptedChecksum_ThrowsInvalidFileException()
    {
        var parser = new FitActivityParser();
        var bytes = CreateFitBytes(FitSport.Running);
        bytes[^1] ^= 0xFF; // the last two bytes of a FIT file are its CRC
        var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidFileException>(() => parser.Parse(stream));

        Assert.Equal("The provided fit file is corrupted", exception.Message);
    }

    [Fact]
    public void Parse_FileWithOneSessionAndOneSplit_ReturnsActivitySummary()
    {
        var parser = new FitActivityParser();
        var bytes = CreateFitBytes(FitSport.Running);
        var stream = new MemoryStream(bytes);
        var result = parser.Parse(stream);

        Assert.Equal(new DateTimeOffset(2026, 10, 9, 16, 1, 58, TimeSpan.Zero) , result.StartTime);
        Assert.Equal(Sport.Run, result.Sport);
        Assert.Single(result.Splits);
        Assert.Equal(200f, result.Splits[0].DistanceInMeters);
    }

    [Fact]
    public void Parse_FileWithTwoSessions_ThrowsInvalidFileException()
    {
        var parser = new FitActivityParser();
        var bytes = CreateFitBytes(FitSport.Running, FitSport.Swimming);
        var stream = new MemoryStream(bytes);
        var exception = Assert.Throws<InvalidFileException>(() => parser.Parse(stream));

        Assert.Equal("The provided file contains more than one session", exception.Message);

    }

    // Builds a tiny but valid FIT activity file in memory with the SDK encoder: no real file needed.
    // The file contains one lap and one session for each sport passed (CreateFitBytes(Running) is a valid
    // single-session file, CreateFitBytes(Running, Swimming) has two sessions).
    private static byte[] CreateFitBytes(params FitSport[] sessionSports)
    {
        var startTime = new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc));
        var output = new MemoryStream();
        var encoder = new Encode(ProtocolVersion.V20);
        encoder.Open(output);

        var fileId = new FileIdMesg();
        fileId.SetType(Dynastream.Fit.File.Activity); // fully qualified: System.IO.File is also in scope
        fileId.SetManufacturer(Manufacturer.Garmin);
        fileId.SetTimeCreated(startTime);
        encoder.Write(fileId);

        var lap = new LapMesg();
        lap.SetStartTime(startTime);
        lap.SetTotalTimerTime(74.086f);
        lap.SetTotalDistance(200f);
        lap.SetIntensity(Intensity.Active);
        encoder.Write(lap);

        for (var i = 0; i < sessionSports.Length; i++)
        {
            var session = new SessionMesg();
            session.SetSport(sessionSports[i]);
            // each following session starts two hours after the previous one
            session.SetStartTime(new FitDateTime(new System.DateTime(2026, 10, 9, 16, 1, 58, DateTimeKind.Utc).AddHours(2 * i)));
            session.SetTotalTimerTime(74.086f);
            session.SetTotalDistance(200f);
            encoder.Write(session);
        }

        encoder.Close();
        return output.ToArray();
    }
}
