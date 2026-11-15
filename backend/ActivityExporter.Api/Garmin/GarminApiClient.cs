using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.Extensions.Options;

namespace ActivityExporter.Api;

/// <summary>
/// Reads the activities from the Personal API of Garmin. Every failure (network, refused key, server error,
/// answer that is not the expected one) becomes a GarminSourceException with a message that can be shown.
/// </summary>
public class GarminApiClient : IGarminActivitySource
{
    private const string SearchPath = "/activitylist-service/activities/search/activities";
    private const string DownloadPath = "/download-service/files/activity/";

    // Garmin sends the activities in pages. If it ignored the page we ask for and kept sending the same
    // full page, this stops the loop (20 pages are 1000 activities at the default size)
    private const int MaxPages = 20;

    private readonly HttpClient _http;
    private readonly GarminTokenProvider _tokens;
    private readonly GarminApiOptions _options;

    public GarminApiClient(HttpClient http, GarminTokenProvider tokens, IOptions<GarminApiOptions> options)
    {
        _http = http;
        _tokens = tokens;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<GarminActivityListItem>> ListActivitiesAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var pageSize = _options.PageSize;
        var startDate = ToDate(from);
        var endDate = ToDate(to);
        var items = new List<GarminActivityListItem>();

        for (var page = 0; page < MaxPages; page++)
        {
            var path = $"{SearchPath}?start={page * pageSize}&limit={pageSize}&startDate={startDate}&endDate={endDate}";
            var json = await RequestAsync(path, content => content.ReadAsStringAsync());
            var rows = ParseRows(json);

            // Garmin filters by day: on the first and the last day it can send activities outside the period
            items.AddRange(rows.Where(item => item.StartTime >= from && item.StartTime <= to));

            if (rows.Count < pageSize)
            {
                return items;
            }
        }

        throw new GarminSourceException("Garmin sent more pages of activities than expected.");
    }

    public async Task<Stream> DownloadFitAsync(long garminActivityId)
    {
        var zip = await RequestAsync($"{DownloadPath}{garminActivityId}", async content =>
        {
            await using var body = await content.ReadAsStreamAsync();
            return await CopyWithLimitAsync(body);
        });

        return await ReadFitFromZipAsync(zip);
    }

    // Sends the GET with the token, checks the answer and lets `read` take what it needs from the body
    private async Task<T> RequestAsync<T>(string path, Func<HttpContent, Task<T>> read)
    {
        var token = await _tokens.GetTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // ResponseHeadersRead: the body is not loaded at once, a big one can be stopped while it arrives
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            CheckStatus(response.StatusCode);
            return await read(response.Content);
        }
        catch (HttpRequestException exception)
        {
            throw new GarminSourceException("Garmin is not reachable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new GarminSourceException("Garmin did not answer in time.", exception);
        }
    }

    private void CheckStatus(HttpStatusCode status)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // The token was refused before its time (for example the key was deleted): forget it
            _tokens.Invalidate();
            throw new GarminSourceException(
                "Garmin refused the request: the API key may be expired or deleted. Create a new one on Garmin Connect.");
        }

        if (status == HttpStatusCode.TooManyRequests)
        {
            throw new GarminSourceException("Garmin says there are too many requests: try again later.");
        }

        if ((int)status is < 200 or > 299)
        {
            throw new GarminSourceException($"Garmin answered with an error (HTTP {(int)status}).");
        }
    }

    private static string ToDate(DateTimeOffset moment) =>
        moment.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static List<GarminActivityListItem> ParseRows(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new GarminSourceException("Garmin sent a list of activities that is not the expected one.");
            }

            return document.RootElement.EnumerateArray().Select(ParseRow).ToList();
        }
        catch (JsonException exception)
        {
            throw new GarminSourceException("Garmin sent an answer that is not valid JSON.", exception);
        }
    }

    private static GarminActivityListItem ParseRow(JsonElement row)
    {
        if (row.ValueKind != JsonValueKind.Object
            || !row.TryGetProperty("activityId", out var id) || !id.TryGetInt64(out var garminId)
            || !TryReadTypeKey(row, out var typeKey)
            || !TryReadStartTime(row, out var startTime))
        {
            throw new GarminSourceException("Garmin sent an activity without id, type or start time.");
        }

        var name = row.TryGetProperty("activityName", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString()
            : null;

        return new GarminActivityListItem(
            garminId,
            string.IsNullOrWhiteSpace(name) ? typeKey : name,
            typeKey,
            startTime,
            TimeSpan.FromSeconds(ReadNumber(row, "duration") ?? 0),
            ReadHeartRate(row, "averageHR"),
            ReadHeartRate(row, "maxHR"),
            RoundToInt(ReadNumber(row, "calories")));
    }

    private static bool TryReadTypeKey(JsonElement row, out string typeKey)
    {
        typeKey = "";
        if (row.TryGetProperty("activityType", out var type)
            && type.ValueKind == JsonValueKind.Object
            && type.TryGetProperty("typeKey", out var key)
            && key.ValueKind == JsonValueKind.String)
        {
            typeKey = key.GetString() ?? "";
        }

        return typeKey.Length > 0;
    }

    // "startTimeGMT" is written like 2026-11-12 10:45:57, without a time zone: it is UTC
    private static bool TryReadStartTime(JsonElement row, out DateTimeOffset startTime)
    {
        startTime = default;
        if (row.TryGetProperty("startTimeGMT", out var element)
            && element.ValueKind == JsonValueKind.String
            && DateTime.TryParseExact(element.GetString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utc))
        {
            startTime = new DateTimeOffset(utc);
            return true;
        }

        return false;
    }

    private static double? ReadNumber(JsonElement row, string property)
    {
        return row.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : null;
    }

    // Garmin writes 0 when there is no heart-rate strap, and a heart rate of 0 does not exist: it means "missing"
    private static int? ReadHeartRate(JsonElement row, string property)
    {
        var rate = RoundToInt(ReadNumber(row, property));
        return rate > 0 ? rate : null;
    }

    // Garmin writes heart rate and calories as 162.0
    private static int? RoundToInt(double? value)
    {
        return value is null ? null : (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    // Reads the .fit file of the ZIP in memory. The name of the entry is only checked for the .fit ending,
    // it is never used as a path; the size is counted while reading, not trusted from the header of the ZIP.
    private static async Task<Stream> ReadFitFromZipAsync(MemoryStream zip)
    {
        try
        {
            using var archive = new ZipArchive(zip, ZipArchiveMode.Read);

            if (archive.Entries.Count != 1)
            {
                throw new GarminSourceException($"The ZIP from Garmin has {archive.Entries.Count} files instead of one.");
            }

            var entry = archive.Entries[0];
            if (!entry.FullName.EndsWith(".fit", StringComparison.OrdinalIgnoreCase))
            {
                throw new GarminSourceException("The file inside the ZIP from Garmin is not a .fit file.");
            }

            await using var content = entry.Open();
            return await CopyWithLimitAsync(content);
        }
        catch (InvalidDataException exception)
        {
            throw new GarminSourceException("The file from Garmin is not a valid ZIP.", exception);
        }
    }

    private static async Task<MemoryStream> CopyWithLimitAsync(Stream source)
    {
        var result = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk)) > 0)
        {
            if (result.Length + read > ActivityImportService.MaxFileSizeInBytes)
            {
                throw new GarminSourceException("The file from Garmin is too large.");
            }

            result.Write(chunk, 0, read);
        }

        result.Position = 0;
        return result;
    }
}
