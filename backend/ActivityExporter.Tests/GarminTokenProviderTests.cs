using System.Net;
using ActivityExporter.Api;
using ActivityExporter.Core.Exceptions;
using static ActivityExporter.Tests.GarminTestData;

namespace ActivityExporter.Tests;

public class GarminTokenProviderTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private static (GarminTokenProvider Provider, FakeHttpHandler Handler, MutableTimeProvider Time) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond, string? apiKey = "test-key")
    {
        var handler = new FakeHttpHandler(respond);
        var time = new MutableTimeProvider { Now = Start };
        return (new GarminTokenProvider(HttpClient(handler), Options(apiKey), time), handler, time);
    }

    [Fact]
    public async Task GetTokenAsync_ExchangesTheKeyForTheToken()
    {
        var token = Jwt(Start.AddHours(24));
        var (provider, handler, _) = Create(_ => Text(token));

        var result = await provider.GetTokenAsync();

        Assert.Equal(token, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(TokenPath, request.Path);
        Assert.Equal("ApiKey test-key", request.Authorization);
    }

    [Fact]
    public async Task GetTokenAsync_AnswerWithQuotesAndNewLine_IsCleaned()
    {
        var token = Jwt(Start.AddHours(24));
        var (provider, _, _) = Create(_ => Text($"\"{token}\"\n"));

        Assert.Equal(token, await provider.GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_CalledTwice_ExchangesTheKeyOnce()
    {
        var (provider, handler, _) = Create(_ => Text(Jwt(Start.AddHours(24))));

        var first = await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.Equal(first, second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetTokenAsync_NearTheExpiry_AsksAgain()
    {
        var (provider, handler, time) = Create(_ => Text(Jwt(Start.AddHours(24))));
        await provider.GetTokenAsync();

        // an hour before the expiry the token is still good
        time.Now = Start.AddHours(23);
        await provider.GetTokenAsync();
        Assert.Single(handler.Requests);

        // five minutes before the expiry it is renewed, so that it does not expire during a sync
        time.Now = Start.AddHours(23).AddMinutes(56);
        await provider.GetTokenAsync();
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetTokenAsync_TokenWithoutReadableExpiry_IsKeptForOneHour()
    {
        var (provider, handler, time) = Create(_ => Text("not-a-jwt"));
        await provider.GetTokenAsync();

        time.Now = Start.AddMinutes(59);
        await provider.GetTokenAsync();
        Assert.Single(handler.Requests);

        time.Now = Start.AddMinutes(61);
        await provider.GetTokenAsync();
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Invalidate_TheNextCallExchangesTheKeyAgain()
    {
        var (provider, handler, _) = Create(_ => Text(Jwt(Start.AddHours(24))));
        await provider.GetTokenAsync();

        provider.Invalidate();
        await provider.GetTokenAsync();

        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTokenAsync_NoKeyConfigured_ThrowsWithoutCallingGarmin(string? apiKey)
    {
        var (provider, handler, _) = Create(_ => Text("unused"), apiKey);

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());

        Assert.Contains("API key", exception.Message);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetTokenAsync_KeyRefused_ThrowsExplainingTheKeyMayBeExpired(HttpStatusCode status)
    {
        var (provider, _, _) = Create(_ => Status(status));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());

        Assert.Contains("API key", exception.Message);
        Assert.Contains("expired", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_ServerError_ThrowsWithTheStatusCode()
    {
        var (provider, _, _) = Create(_ => Status(HttpStatusCode.InternalServerError));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());

        Assert.Contains("500", exception.Message);
    }

    [Fact]
    public async Task GetTokenAsync_GarminNotReachable_Throws()
    {
        var (provider, _, _) = Create(_ => throw new HttpRequestException("no network"));

        var exception = await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task GetTokenAsync_EmptyAnswer_Throws()
    {
        var (provider, _, _) = Create(_ => Text("  \n"));

        await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_AfterAFailure_TriesAgainNextTime()
    {
        var calls = 0;
        var (provider, _, _) = Create(_ => ++calls == 1
            ? Status(HttpStatusCode.InternalServerError)
            : Text(Jwt(Start.AddHours(24))));

        await Assert.ThrowsAsync<GarminSourceException>(() => provider.GetTokenAsync());
        var token = await provider.GetTokenAsync();

        Assert.NotEmpty(token);
    }
}
