using System.Net;
using System.Web;
using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using static ActivityExporter.Tests.GarminTestData;

namespace ActivityExporter.Tests;

public class GarminApiClientTests
{
    private static readonly DateTimeOffset From = new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    // The token requests are answered here, `api` answers all the others
    private static (GarminApiClient Client, FakeHttpHandler Handler, string Token) Create(
        Func<HttpRequestMessage, HttpResponseMessage> api, int pageSize = 50)
    {
        var time = new MutableTimeProvider();
        var token = Jwt(time.Now.AddHours(24));
        var handler = new FakeHttpHandler(request =>
            request.RequestUri!.AbsolutePath == TokenPath ? Text(token) : api(request));
        var options = Options(pageSize: pageSize);
        var http = HttpClient(handler);
        var tokens = new GarminTokenProvider(http, options, time);
        return (new GarminApiClient(http, tokens, options), handler, token);
    }

    private static IEnumerable<RecordedRequest> Searches(FakeHttpHandler handler) =>
        handler.Requests.Where(request => request.Path == SearchPath);

    // ---- the list of activities

    [Fact]
    public async Task ListActivitiesAsync_AsksGarminForThePeriodWithTheToken()
    {
        var (client, handler, token) = Create(_ => Text("[]"));

        await client.ListActivitiesAsync(From, To);

        var search = Assert.Single(Searches(handler));
        Assert.Equal(HttpMethod.Get, search.Method);
        Assert.Equal($"Bearer {token}", search.Authorization);
        var query = HttpUtility.ParseQueryString(search.Query);
        Assert.Equal("0", query["start"]);
        Assert.Equal("50", query["limit"]);
        Assert.Equal("2026-10-16", query["startDate"]);
        Assert.Equal("2026-11-15", query["endDate"]);
    }

    [Fact]
    public async Task ListActivitiesAsync_MapsTheFieldsOfTheActivities()
    {
        var (client, _, _) = Create(_ => Text(RowsJson(
            Row(25053166010, "2026-11-12 10:45:57", "running", "Morning run", 2351.18896484375, 162.0, 178.0, 410.0))));

        var items = await client.ListActivitiesAsync(From, To);

        var item = Assert.Single(items);
        Assert.Equal(new GarminActivityListItem(
            25053166010,
            "Morning run",
            "running",
            new DateTimeOffset(2026, 11, 12, 10, 45, 57, TimeSpan.Zero),
            TimeSpan.FromSeconds(2351.18896484375),
            162, 178, 410), item);
    }

    [Fact]
    public async Task ListActivitiesAsync_StartTimeIsTheUtcOne()
    {
        var (client, _, _) = Create(_ => Text(RowsJson(Row(1, "2026-11-12 10:45:57"))));

        var item = Assert.Single(await client.ListActivitiesAsync(From, To));

        Assert.Equal(TimeSpan.Zero, item.StartTime.Offset);
    }

    [Fact]
    public async Task ListActivitiesAsync_HeartRateWithDecimals_IsRounded()
    {
        var (client, _, _) = Create(_ => Text(RowsJson(Row(1, "2026-11-12 10:45:57", averageHr: 161.6, maxHr: 177.4, calories: 410.5))));

        var item = Assert.Single(await client.ListActivitiesAsync(From, To));

        Assert.Equal(162, item.AvgHR);
        Assert.Equal(177, item.MaxHR);
        // half rounds away from zero (411), not to the even number (410)
        Assert.Equal(411, item.Calories);
    }

    [Fact]
    public async Task ListActivitiesAsync_HeartRateZero_MeansNoHeartRateStrapAndIsNull()
    {
        // seen in the real data: an indoor ride without a strap has "averageHR": 0.0, not a missing field
        var (client, _, _) = Create(_ => Text(RowsJson(Row(1, "2026-11-12 10:45:57", averageHr: 0.0, maxHr: 0.0, calories: 45.0))));

        var item = Assert.Single(await client.ListActivitiesAsync(From, To));

        Assert.Null(item.AvgHR);
        Assert.Null(item.MaxHR);
        Assert.Equal(45, item.Calories);
    }

    [Fact]
    public async Task ListActivitiesAsync_MissingOptionalFields_AreNull()
    {
        var (client, _, _) = Create(_ => Text(RowsJson(
            Row(1, "2026-11-12 10:45:57", duration: null, averageHr: null, maxHr: null, calories: null))));

        var item = Assert.Single(await client.ListActivitiesAsync(From, To));

        Assert.Null(item.AvgHR);
        Assert.Null(item.MaxHR);
        Assert.Null(item.Calories);
        Assert.Equal(TimeSpan.Zero, item.Duration);
    }

    [Fact]
    public async Task ListActivitiesAsync_ActivityWithoutName_UsesTheTypeAsName()
    {
        var (client, _, _) = Create(_ => Text(RowsJson(Row(1, "2026-11-12 10:45:57", "hiit", name: null))));

        var item = Assert.Single(await client.ListActivitiesAsync(From, To));

        Assert.Equal("hiit", item.Name);
    }

    [Fact]
    public async Task ListActivitiesAsync_ReadsEveryPage()
    {
        var (client, handler, _) = Create(request =>
        {
            var start = HttpUtility.ParseQueryString(request.RequestUri!.Query)["start"];
            return start == "0"
                ? Text(RowsJson(Row(1, "2026-11-14 10:00:00"), Row(2, "2026-11-13 10:00:00")))
                : Text(RowsJson(Row(3, "2026-11-12 10:00:00")));
        }, pageSize: 2);

        var items = await client.ListActivitiesAsync(From, To);

        Assert.Equal([1L, 2L, 3L], items.Select(item => item.GarminActivityId));
        var starts = Searches(handler).Select(search => HttpUtility.ParseQueryString(search.Query)["start"]);
        Assert.Equal(["0", "2"], starts);
    }

    [Fact]
    public async Task ListActivitiesAsync_LastPageExactlyFull_AsksOneMorePage()
    {
        var (client, handler, _) = Create(request =>
        {
            var start = HttpUtility.ParseQueryString(request.RequestUri!.Query)["start"];
            return start == "0"
                ? Text(RowsJson(Row(1, "2026-11-14 10:00:00"), Row(2, "2026-11-13 10:00:00")))
                : Text("[]");
        }, pageSize: 2);

        var items = await client.ListActivitiesAsync(From, To);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, Searches(handler).Count());
    }

    [Fact]
    public async Task ListActivitiesAsync_ActivitiesOutsideThePeriod_AreLeftOut()
    {
        // Garmin filters by day: on the first and the last day it also sends what is outside the period
        var (client, _, _) = Create(_ => Text(RowsJson(
            Row(1, "2026-10-15 23:59:59"),
            Row(2, "2026-10-16 00:00:00"),
            Row(3, "2026-11-15 10:00:00"),
            Row(4, "2026-11-15 10:00:01"))));

        var items = await client.ListActivitiesAsync(From, To);

        Assert.Equal([2L, 3L], items.Select(item => item.GarminActivityId).Order());
    }

    [Fact]
    public async Task ListActivitiesAsync_GarminKeepsSendingFullPages_StopsWithAnError()
    {
        var (client, handler, _) = Create(_ => Text(RowsJson(Row(1, "2026-11-14 10:00:00"), Row(2, "2026-11-13 10:00:00"))), pageSize: 2);

        await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));

        Assert.InRange(Searches(handler).Count(), 1, 100);
    }

    [Fact]
    public async Task ListActivitiesAsync_ServerError_ThrowsWithTheStatusCode()
    {
        var (client, _, _) = Create(_ => Status(HttpStatusCode.InternalServerError));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));

        Assert.Contains("500", exception.Message);
    }

    [Fact]
    public async Task ListActivitiesAsync_TooManyRequests_ThrowsSayingToTryLater()
    {
        var (client, _, _) = Create(_ => Status(HttpStatusCode.TooManyRequests));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));

        Assert.Contains("too many", exception.Message);
    }

    [Fact]
    public async Task ListActivitiesAsync_Unauthorized_ThrowsAboutTheKeyAndTheNextCallGetsANewToken()
    {
        var calls = 0;
        var (client, handler, _) = Create(_ => ++calls == 1 ? Status(HttpStatusCode.Unauthorized) : Text("[]"));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));
        await client.ListActivitiesAsync(From, To);

        Assert.Contains("API key", exception.Message);
        // the token that Garmin refused is forgotten: the second call exchanged the key again
        Assert.Equal(2, handler.Requests.Count(request => request.Path == TokenPath));
    }

    [Fact]
    public async Task ListActivitiesAsync_GarminNotReachable_Throws()
    {
        var (client, _, _) = Create(_ => throw new HttpRequestException("no network"));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("{}")]
    [InlineData("[{\"activityName\":\"no id, no type, no start\"}]")]
    public async Task ListActivitiesAsync_UnexpectedAnswer_Throws(string body)
    {
        var (client, _, _) = Create(_ => Text(body));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.ListActivitiesAsync(From, To));
    }

    // ---- the download of the .fit file

    private static byte[] FitBytes() => [14, 16, 108, 8, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    [Fact]
    public async Task DownloadFitAsync_ReturnsTheFitFileInsideTheZip()
    {
        var zip = Zip(("123_ACTIVITY.fit", FitBytes()));
        var (client, handler, token) = Create(_ => Bytes(zip));

        await using var stream = await client.DownloadFitAsync(123);

        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        Assert.Equal(FitBytes(), copy.ToArray());
        var request = Assert.Single(handler.Requests, request => request.Path != TokenPath);
        Assert.Equal("/download-service/files/activity/123", request.Path);
        Assert.Equal($"Bearer {token}", request.Authorization);
    }

    [Fact]
    public async Task DownloadFitAsync_ReturnedStreamStartsAtTheBeginning()
    {
        var (client, _, _) = Create(_ => Bytes(Zip(("1.fit", FitBytes()))));

        await using var stream = await client.DownloadFitAsync(1);

        Assert.Equal(0, stream.Position);
        Assert.Equal(FitBytes().Length, stream.Length);
    }

    [Fact]
    public async Task DownloadFitAsync_ZipWithMoreThanOneFile_Throws()
    {
        var (client, _, _) = Create(_ => Bytes(Zip(("1.fit", FitBytes()), ("2.fit", FitBytes()))));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));
    }

    [Fact]
    public async Task DownloadFitAsync_EmptyZip_Throws()
    {
        var (client, _, _) = Create(_ => Bytes(Zip()));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));
    }

    [Fact]
    public async Task DownloadFitAsync_FileThatIsNotFit_Throws()
    {
        var (client, _, _) = Create(_ => Bytes(Zip(("notes.txt", FitBytes()))));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));
    }

    [Fact]
    public async Task DownloadFitAsync_FitExactlyAtTheLimit_IsAccepted()
    {
        var zip = Zip(("1.fit", new byte[ActivityImportService.MaxFileSizeInBytes]));
        var (client, _, _) = Create(_ => Bytes(zip));

        await using var stream = await client.DownloadFitAsync(1);

        Assert.Equal(ActivityImportService.MaxFileSizeInBytes, stream.Length);
    }

    [Fact]
    public async Task DownloadFitAsync_FitOverTheLimit_Throws()
    {
        // a small ZIP (zeros compress very well) that becomes more than 10 MB: a "zip bomb" in small
        var zip = Zip(("1.fit", new byte[ActivityImportService.MaxFileSizeInBytes + 1]));
        var (client, _, _) = Create(_ => Bytes(zip));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));

        Assert.Contains("too large", exception.Message);
    }

    [Fact]
    public async Task DownloadFitAsync_AnswerOverTheLimit_Throws()
    {
        var (client, _, _) = Create(_ => Bytes(new byte[ActivityImportService.MaxFileSizeInBytes + 1]));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));

        Assert.Contains("too large", exception.Message);
    }

    [Fact]
    public async Task DownloadFitAsync_NotAZip_Throws()
    {
        var (client, _, _) = Create(_ => Bytes([1, 2, 3]));

        await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));
    }

    [Fact]
    public async Task DownloadFitAsync_NameInsideTheZipWithAPath_IsNeverUsedAsAPath()
    {
        // Only the bytes are read, in memory: the name of the entry is just checked for the .fit ending
        var (client, _, _) = Create(_ => Bytes(Zip(("../../outside/evil.fit", FitBytes()))));

        await using var stream = await client.DownloadFitAsync(1);

        Assert.Equal(FitBytes().Length, stream.Length);
    }

    [Fact]
    public async Task DownloadFitAsync_ActivityNotFound_Throws()
    {
        var (client, _, _) = Create(_ => Status(HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => client.DownloadFitAsync(1));

        Assert.Contains("404", exception.Message);
    }
}
