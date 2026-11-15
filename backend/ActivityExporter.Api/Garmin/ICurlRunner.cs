namespace ActivityExporter.Api;

/// <summary>
/// Runs the curl program with a configuration and gives back what it printed
/// </summary>
public interface ICurlRunner
{
    /// <param name="config">The curl configuration, sent to curl on its standard input</param>
    /// <exception cref="HttpRequestException">When curl cannot be started or prints too much</exception>
    /// <exception cref="OperationCanceledException">When the token is cancelled: curl is stopped</exception>
    Task<CurlResult> RunAsync(string config, CancellationToken cancellationToken);
}

/// <param name="ExitCode">0 when curl did its job; see "man curl" for the other codes</param>
/// <param name="Output">Everything curl printed on its standard output, as bytes</param>
/// <param name="Error">What curl printed on its standard error</param>
public record CurlResult(int ExitCode, byte[] Output, string Error);
