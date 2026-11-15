using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ActivityExporter.Api;

namespace ActivityExporter.Tests;

// A small web server on this computer, to see what the real curl program does with it
sealed class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();

    public string BaseUrl { get; }
    public ConcurrentQueue<(string Method, string PathAndQuery, string? Authorization)> Requests { get; } = new();

    public LocalHttpServer(Func<HttpListenerContext, Task> handle)
    {
        BaseUrl = $"http://127.0.0.1:{FreePort()}";
        _listener.Prefixes.Add(BaseUrl + "/");
        _listener.Start();
        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    Requests.Enqueue((context.Request.HttpMethod, context.Request.Url!.PathAndQuery,
                        context.Request.Headers["Authorization"]));
                    try
                    {
                        await handle(context);
                    }
                    catch
                    {
                        // the client went away (a test of timeouts and cancellations)
                    }
                    finally
                    {
                        try { context.Response.Close(); } catch { }
                    }
                });
            }
        });
    }

    public static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose() => _listener.Close();
}

// These tests run the curl program of the computer, as the app does
public class ProcessCurlRunnerTests
{
    private static HttpClient CreateClient(string baseUrl, TimeSpan? maxTime = null, long maxOutputBytes = 20 * 1024 * 1024) =>
        new(new CurlHttpMessageHandler(new ProcessCurlRunner(maxOutputBytes: maxOutputBytes), maxTime))
        {
            BaseAddress = new Uri(baseUrl)
        };

    [Fact]
    public async Task Get_ReturnsTheStatusAndTheBodyByteForByte()
    {
        var body = Enumerable.Range(0, 4096).Select(value => (byte)(value % 256)).ToArray();
        using var server = new LocalHttpServer(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(body);
        });
        using var client = CreateClient(server.BaseUrl);

        using var response = await client.GetAsync("/file");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(body, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Post_SendsTheMethodThePathAndTheHeader()
    {
        using var server = new LocalHttpServer(context =>
        {
            context.Response.StatusCode = 201;
            return Task.CompletedTask;
        });
        using var client = CreateClient(server.BaseUrl);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/token?x=1");
        request.Headers.TryAddWithoutValidation("Authorization", "ApiKey secret-1");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var seen = Assert.Single(server.Requests);
        Assert.Equal("POST", seen.Method);
        Assert.Equal("/token?x=1", seen.PathAndQuery);
        Assert.Equal("ApiKey secret-1", seen.Authorization);
    }

    [Fact]
    public async Task Get_ErrorStatusWithABody_IsReturnedWithItsBody()
    {
        using var server = new LocalHttpServer(async context =>
        {
            context.Response.StatusCode = 404;
            await context.Response.OutputStream.WriteAsync("nothing here"u8.ToArray());
        });
        using var client = CreateClient(server.BaseUrl);

        using var response = await client.GetAsync("/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("nothing here", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_NobodyListening_ThrowsHttpRequestException()
    {
        using var client = CreateClient($"http://127.0.0.1:{LocalHttpServer.FreePort()}");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/x"));
    }

    [Fact]
    public async Task Get_ServerTooSlow_ThrowsTaskCanceledException()
    {
        using var server = new LocalHttpServer(context => Task.Delay(TimeSpan.FromSeconds(8)));
        using var client = CreateClient(server.BaseUrl, maxTime: TimeSpan.FromSeconds(1));
        var clock = Stopwatch.StartNew();

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync("/slow"));

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(6), $"it waited {clock.Elapsed}");
    }

    [Fact]
    public async Task Get_Cancelled_StopsCurlAndThrows()
    {
        using var server = new LocalHttpServer(context => Task.Delay(TimeSpan.FromSeconds(8)));
        using var client = CreateClient(server.BaseUrl);
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var clock = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/slow", source.Token));

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"it waited {clock.Elapsed}");
    }

    [Fact]
    public async Task Get_AnswerBiggerThanTheLimit_ThrowsHttpRequestException()
    {
        using var server = new LocalHttpServer(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(new byte[3_000_000]);
        });
        using var client = CreateClient(server.BaseUrl, maxOutputBytes: 1_000_000);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/big"));

        Assert.Contains("too large", exception.Message);
    }

    // what curl prints after the body of 1000 bytes: a new line, the marker and the status code
    private const int OutputOfAThousandByteBody = 1000 + 20;

    [Fact]
    public async Task Get_AnswerExactlyAtTheLimit_IsAccepted()
    {
        using var server = new LocalHttpServer(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(new byte[1000]);
        });
        using var client = CreateClient(server.BaseUrl, maxOutputBytes: OutputOfAThousandByteBody);

        using var response = await client.GetAsync("/exact");

        Assert.Equal(1000, (await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task Get_AnswerOneByteOverTheLimit_ThrowsHttpRequestException()
    {
        using var server = new LocalHttpServer(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(new byte[1000]);
        });
        using var client = CreateClient(server.BaseUrl, maxOutputBytes: OutputOfAThousandByteBody - 1);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/over"));
    }

    [Fact]
    public async Task Cancelled_TheCurlProcessIsKilled()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // the "curl" of this test is a shell script
        }

        var folder = Directory.CreateTempSubdirectory("fake-curl");
        try
        {
            // a "curl" that writes its process id and then waits for 30 seconds
            var script = Path.Combine(folder.FullName, "slow-curl");
            File.WriteAllText(script, "#!/bin/sh\necho $$ > \"$0.pid\"\nsleep 30\n");
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            using var client = new HttpClient(new CurlHttpMessageHandler(new ProcessCurlRunner(script)))
            {
                BaseAddress = new Uri("https://garmin.test")
            };
            using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/x", source.Token));

            var pid = int.Parse(File.ReadAllText(script + ".pid").Trim());
            Assert.True(IsGone(pid), "the curl process is still running after the cancellation");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    // Waits a little: the process is killed and the system needs a moment to let it go
    private static bool IsGone(int pid)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                if (process.HasExited)
                {
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return true; // no process with this id
            }

            Thread.Sleep(100);
        }

        return false;
    }

    [Fact]
    public async Task CurlNotFound_ThrowsHttpRequestExceptionThatSaysSo()
    {
        var runner = new ProcessCurlRunner("/this/curl/does/not/exist");
        using var client = new HttpClient(new CurlHttpMessageHandler(runner)) { BaseAddress = new Uri("http://127.0.0.1:1") };

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/x"));

        Assert.Contains("could not be started", exception.Message);
    }

    [Fact]
    public async Task TheKeyTravelsOnStandardInputAndNotInTheArguments()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // the "curl" of this test is a shell script
        }

        var folder = Directory.CreateTempSubdirectory("fake-curl");
        try
        {
            var script = Path.Combine(folder.FullName, "fake-curl");
            File.WriteAllText(script, """
                #!/bin/sh
                printf '%s\n' "$@" > "$0.args"
                cat > "$0.stdin"
                printf 'ok\n__curl_status__:200'
                """.ReplaceLineEndings("\n"));
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            using var client = new HttpClient(new CurlHttpMessageHandler(new ProcessCurlRunner(script)))
            {
                BaseAddress = new Uri("https://garmin.test")
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, "/token");
            request.Headers.TryAddWithoutValidation("Authorization", "ApiKey the-secret-key");

            using var response = await client.SendAsync(request);

            Assert.Equal("ok", await response.Content.ReadAsStringAsync());
            // "-q" first: the .curlrc of the user must not change what curl does
            Assert.Equal(["-q", "--config", "-"], File.ReadAllLines(script + ".args"));
            Assert.DoesNotContain("the-secret-key", File.ReadAllText(script + ".args"));
            Assert.Contains("the-secret-key", File.ReadAllText(script + ".stdin"));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
