using System.Net;
using System.Text;

namespace ActivityExporter.Api;

/// <summary>
/// Sends the HTTP requests by running the curl program instead of using .NET.
/// Why: the Personal API of Garmin accepts only TLS 1.3 and .NET 8 on macOS does not have it. It only
/// sends requests without a body (GET, and the POST of the token), which is all the Garmin client needs.
/// </summary>
public class CurlHttpMessageHandler : HttpMessageHandler
{
    // curl prints this after the body (see "write-out" in the configuration): a new line, the marker, the status code
    private const string StatusMarker = "\n__curl_status__:";
    private static readonly byte[] StatusMarkerBytes = Encoding.ASCII.GetBytes(StatusMarker);

    private readonly ICurlRunner _runner;
    private readonly TimeSpan _maxTime;

    /// <param name="maxTime">curl stops by itself after this time, whatever the HttpClient timeout is</param>
    public CurlHttpMessageHandler(ICurlRunner runner, TimeSpan? maxTime = null)
    {
        _runner = runner;
        _maxTime = maxTime ?? TimeSpan.FromSeconds(60);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            throw new NotSupportedException("The curl handler sends requests without a body only.");
        }

        var config = BuildConfig(request);
        var result = await _runner.RunAsync(config, cancellationToken);
        return ToResponse(request, result);
    }

    // The configuration goes to curl on its standard input: headers with the key or the token stay out of the arguments
    private string BuildConfig(HttpRequestMessage request)
    {
        var uri = request.RequestUri ?? throw new HttpRequestException("The request has no address.");

        var lines = new List<string>
        {
            "silent",
            "show-error",
            $"max-time = {(int)Math.Ceiling(_maxTime.TotalSeconds)}",
            "connect-timeout = 15",
            $"request = {Quote(request.Method.Method)}",
            $"url = {Quote(uri.AbsoluteUri)}"
        };

        foreach (var header in request.Headers)
        {
            lines.Add($"header = {Quote($"{header.Key}: {string.Join(", ", header.Value)}")}");
        }

        // HttpClient sends "Content-Length: 0" for a POST without a body; curl sends nothing, and a strict
        // server answers 411 (Length Required)
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            lines.Add("header = \"Content-Length: 0\"");
        }

        // After the body: a new line (written as \n, the escape of the configuration) and the status code
        lines.Add("write-out = \"\\n__curl_status__:%{http_code}\"");

        return string.Join("\n", lines) + "\n";
    }

    // A value is written between double quotes. A line break would let it start a new line of the
    // configuration, for example "output = ...", so it is refused instead of being cleaned.
    private static string Quote(string value)
    {
        if (value.Any(character => character is '\r' or '\n' or '\0'))
        {
            throw new HttpRequestException("A line break cannot go in a header or in the address.");
        }

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static HttpResponseMessage ToResponse(HttpRequestMessage request, CurlResult result)
    {
        // 28 is "operation timed out": same meaning as the timeout of the HttpClient
        if (result.ExitCode == 28)
        {
            throw new TaskCanceledException("curl stopped because the time limit was reached.");
        }

        if (result.ExitCode != 0)
        {
            throw new HttpRequestException($"curl failed (exit {result.ExitCode}): {result.Error.Trim()}");
        }

        // The status is written after the body: the last marker is the one of curl, the body may contain the same text
        var markerAt = result.Output.AsSpan().LastIndexOf(StatusMarkerBytes);
        if (markerAt < 0)
        {
            throw new HttpRequestException("curl printed an answer that is not the expected one.");
        }

        var statusText = Encoding.ASCII.GetString(result.Output.AsSpan(markerAt + StatusMarkerBytes.Length)).Trim();
        if (!int.TryParse(statusText, out var status) || status is < 100 or > 599)
        {
            throw new HttpRequestException($"curl printed an HTTP status that is not valid: '{statusText}'.");
        }

        return new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new ByteArrayContent(result.Output[..markerAt]),
            RequestMessage = request
        };
    }
}
