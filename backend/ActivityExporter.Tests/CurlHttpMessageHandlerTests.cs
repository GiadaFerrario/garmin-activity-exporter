using System.Net;
using System.Text;
using ActivityExporter.Api;

namespace ActivityExporter.Tests;

class FakeCurlRunner : ICurlRunner
{
    public string? Config { get; private set; }
    public int Calls { get; private set; }
    public Func<CurlResult> Result { get; set; } = () => Output("", 200);

    // When it is set, it runs instead of Result: it can wait for the cancellation token
    public Func<CancellationToken, Task<CurlResult>>? Behavior { get; set; }

    public Task<CurlResult> RunAsync(string config, CancellationToken cancellationToken)
    {
        Calls++;
        Config = config;
        return Behavior is not null ? Behavior(cancellationToken) : Task.FromResult(Result());
    }

    // What curl prints with the configuration of the handler: the body, then a new line and the status
    public static CurlResult Output(string body, int status) =>
        new(0, [.. Encoding.UTF8.GetBytes(body), .. Encoding.ASCII.GetBytes($"\n__curl_status__:{status}")], "");

    public static CurlResult Output(byte[] body, int status) =>
        new(0, [.. body, .. Encoding.ASCII.GetBytes($"\n__curl_status__:{status}")], "");
}

public class CurlHttpMessageHandlerTests
{
    private static (HttpClient Client, FakeCurlRunner Runner) Create(TimeSpan? maxTime = null)
    {
        var runner = new FakeCurlRunner();
        var client = new HttpClient(new CurlHttpMessageHandler(runner, maxTime)) { BaseAddress = new Uri("https://garmin.test") };
        return (client, runner);
    }

    [Fact]
    public async Task Get_SendsTheMethodTheUrlAndTheHeadersInTheConfiguration()
    {
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output("[]", 200);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/a/b?x=1&y=2");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer the-token");

        using var response = await client.SendAsync(request);

        var config = runner.Config!;
        Assert.Contains("request = \"GET\"", config);
        Assert.Contains("url = \"https://garmin.test/a/b?x=1&y=2\"", config);
        Assert.Contains("header = \"Authorization: Bearer the-token\"", config);
        Assert.Contains("silent", config);
    }

    [Fact]
    public async Task Post_UsesThePostMethodAndTheKeyHeader()
    {
        var (client, runner) = Create();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/token");
        request.Headers.TryAddWithoutValidation("Authorization", "ApiKey the-key");

        await client.SendAsync(request);

        Assert.Contains("request = \"POST\"", runner.Config);
        Assert.Contains("header = \"Authorization: ApiKey the-key\"", runner.Config);
    }

    [Fact]
    public async Task Send_ReturnsTheStatusAndTheBodyThatCurlPrinted()
    {
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output("hello", 200);

        using var response = await client.GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("hello", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    public async Task Send_AnyStatusCodeIsReturnedAsItIs(int status)
    {
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output("", status);

        using var response = await client.GetAsync("/x");

        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task Send_BinaryBody_IsReturnedByteForByte()
    {
        var body = Enumerable.Range(0, 256).Select(value => (byte)value).Concat(new byte[] { 10, 13, 10 }).ToArray();
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output(body, 200);

        using var response = await client.GetAsync("/x");

        Assert.Equal(body, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Send_BodyThatEndsWithANewLine_KeepsIt()
    {
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output("line\n", 200);

        using var response = await client.GetAsync("/x");

        Assert.Equal("line\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Send_BodyThatContainsTheStatusMarker_UsesTheLastOne()
    {
        var (client, runner) = Create();
        runner.Result = () => FakeCurlRunner.Output("a\n__curl_status__:999\nb", 200);

        using var response = await client.GetAsync("/x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("a\n__curl_status__:999\nb", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Send_ConfigurationHasTheTimeLimit()
    {
        var (client, runner) = Create(maxTime: TimeSpan.FromSeconds(42));

        await client.GetAsync("/x");

        Assert.Contains("max-time = 42", runner.Config);
    }

    [Fact]
    public async Task Send_WhenTheCallerCancels_TheRunnerIsCancelled()
    {
        var (client, runner) = Create();
        // a runner that waits for ever, until the token it receives is cancelled
        runner.Behavior = async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return FakeCurlRunner.Output("", 200);
        };
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/x", source.Token));
    }

    [Fact]
    public async Task Post_WithoutABody_SaysSoWithAContentLengthOfZero()
    {
        // HttpClient does it for a POST without a body; curl does not, and some servers answer 411 without it
        var (client, runner) = Create();

        await client.PostAsync("/x", null);

        Assert.Contains("header = \"Content-Length: 0\"", runner.Config);
    }

    [Fact]
    public async Task Get_HasNoContentLengthHeader()
    {
        var (client, runner) = Create();

        await client.GetAsync("/x");

        Assert.DoesNotContain("Content-Length", runner.Config);
    }

    [Fact]
    public async Task Send_CurlFails_ThrowsHttpRequestExceptionWithTheExitCodeAndTheMessage()
    {
        var (client, runner) = Create();
        runner.Result = () => new CurlResult(35, [], "curl: (35) handshake failure\n");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/x"));

        Assert.Contains("35", exception.Message);
        Assert.Contains("handshake failure", exception.Message);
    }

    [Fact]
    public async Task Send_CurlTimesOut_ThrowsTaskCanceledException()
    {
        var (client, runner) = Create();
        runner.Result = () => new CurlResult(28, [], "curl: (28) Operation timed out");

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync("/x"));
    }

    [Fact]
    public async Task Send_OutputWithoutTheStatusMarker_ThrowsHttpRequestException()
    {
        var (client, runner) = Create();
        runner.Result = () => new CurlResult(0, Encoding.UTF8.GetBytes("something else"), "");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/x"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("99")]
    public async Task Send_StatusThatIsNotAnHttpStatus_ThrowsHttpRequestException(string status)
    {
        var (client, runner) = Create();
        runner.Result = () => new CurlResult(0, Encoding.ASCII.GetBytes($"body\n__curl_status__:{status}"), "");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/x"));
    }

    [Fact]
    public async Task Send_QuotesAndBackslashesInAHeader_AreEscapedInTheConfiguration()
    {
        var (client, runner) = Create();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/x");
        request.Headers.TryAddWithoutValidation("X-Test", "a\"b\\c");

        await client.SendAsync(request);

        Assert.Contains("header = \"X-Test: a\\\"b\\\\c\"", runner.Config);
    }

    [Fact]
    public async Task Send_HeaderWithALineBreak_IsRejectedAndCurlIsNotRun()
    {
        // A line break would let a value start a new line of the configuration (for example an "output = ..."
        // line), so it is refused. HttpClient lets a folded line through: "\r\n" followed by a space.
        var (client, runner) = Create();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/x");
        Assert.True(request.Headers.TryAddWithoutValidation("X-Evil", "first\r\n output = \"/tmp/evil\""));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));

        Assert.Equal(0, runner.Calls);
    }

    [Fact]
    public async Task Send_RequestWithABody_IsNotSupported()
    {
        var (client, runner) = Create();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/x") { Content = new StringContent("data") };

        await Assert.ThrowsAsync<NotSupportedException>(() => client.SendAsync(request));

        Assert.Equal(0, runner.Calls);
    }

    [Fact]
    public void ShouldUseCurl_NotConfigured_IsOnlyForMacOs()
    {
        var options = new GarminApiOptions();

        Assert.True(options.ShouldUseCurl(isMacOs: true));
        Assert.False(options.ShouldUseCurl(isMacOs: false));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ShouldUseCurl_Configured_TheConfigurationWinsOverThePlatform(bool useCurl, bool isMacOs)
    {
        var options = new GarminApiOptions { UseCurl = useCurl };

        Assert.Equal(useCurl, options.ShouldUseCurl(isMacOs));
    }
}
