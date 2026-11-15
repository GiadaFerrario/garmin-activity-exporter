using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using ActivityExporter.Api;
using Microsoft.Extensions.Options;

namespace ActivityExporter.Tests;

// Everything that the tests of the Garmin client share: a fake HTTP server, a clock that can be moved,
// a way to build tokens, answers and ZIP files.

class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public List<RecordedRequest> Requests { get; } = [];

    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.AbsolutePath, request.RequestUri.Query,
            request.Headers.Authorization?.ToString()));
        return Task.FromResult(_respond(request));
    }
}

record RecordedRequest(HttpMethod Method, string Path, string Query, string? Authorization);

class MutableTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

static class GarminTestData
{
    public const string TokenPath = "/personalapitoken-service/personal-api/personal-api-key/token";
    public const string SearchPath = "/activitylist-service/activities/search/activities";

    public static IOptions<GarminApiOptions> Options(string? apiKey = "test-key", int pageSize = 50) =>
        Microsoft.Extensions.Options.Options.Create(new GarminApiOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://garmin.test",
            PageSize = pageSize
        });

    public static HttpClient HttpClient(FakeHttpHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://garmin.test") };

    private static string Base64Url(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // A token like the ones of Garmin: a JWT, three parts, the second one says when it expires
    public static string Jwt(DateTimeOffset expiry, string signature = "signature") =>
        $"{Base64Url("{\"alg\":\"none\"}")}.{Base64Url($"{{\"exp\":{expiry.ToUnixTimeSeconds()}}}")}.{signature}";

    public static HttpResponseMessage Text(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body) };

    public static HttpResponseMessage Status(HttpStatusCode status) => new(status);

    public static HttpResponseMessage Bytes(byte[] content) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };

    // One activity of the list of Garmin, with only the fields that the client reads
    public static Dictionary<string, object?> Row(
        long id,
        string startGmt,
        string typeKey = "running",
        string? name = "Morning run",
        double? duration = 2351.18896484375,
        double? averageHr = 162.0,
        double? maxHr = 178.0,
        double? calories = 410.0)
    {
        var row = new Dictionary<string, object?>
        {
            ["activityId"] = id,
            ["activityType"] = new { typeId = 1, typeKey, parentTypeId = 17 },
            ["startTimeGMT"] = startGmt,
            // fields that the client does not need, as in the real answer
            ["ownerFullName"] = "Someone",
            ["locationName"] = "Somewhere"
        };
        if (name is not null) row["activityName"] = name;
        if (duration is not null) row["duration"] = duration;
        if (averageHr is not null) row["averageHR"] = averageHr;
        if (maxHr is not null) row["maxHR"] = maxHr;
        if (calories is not null) row["calories"] = calories;
        return row;
    }

    public static string RowsJson(params Dictionary<string, object?>[] rows) => JsonSerializer.Serialize(rows);

    public static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }
}
