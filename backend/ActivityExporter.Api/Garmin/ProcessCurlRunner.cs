using System.ComponentModel;
using System.Diagnostics;

namespace ActivityExporter.Api;

/// <summary>
/// Runs the real curl program of the computer
/// </summary>
public class ProcessCurlRunner : ICurlRunner
{
    private readonly string _curlPath;
    private readonly long _maxOutputBytes;

    /// <param name="curlPath">The name of the program if it is in the PATH, or its full path</param>
    /// <param name="maxOutputBytes">curl is stopped if it prints more than this (the body of the answer and a few bytes)</param>
    public ProcessCurlRunner(string curlPath = "curl", long maxOutputBytes = 20 * 1024 * 1024)
    {
        _curlPath = curlPath;
        _maxOutputBytes = maxOutputBytes;
    }

    public async Task<CurlResult> RunAsync(string config, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(_curlPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // -q must be the first argument: the .curlrc of the user must not change what curl does.
        // The configuration, with the key, comes from the standard input ("-"): it is not in the arguments,
        // which any program of the computer can read in the list of the processes.
        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add("--config");
        startInfo.ArgumentList.Add("-");

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new HttpRequestException($"curl could not be started ('{_curlPath}'): {exception.Message}", exception);
        }

        try
        {
            // Reading starts before writing: curl must never wait for us while we wait for it
            var output = ReadOutputAsync(process, cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await WriteConfigAsync(process, config);
            await process.WaitForExitAsync(cancellationToken);
            return new CurlResult(process.ExitCode, await output, await error);
        }
        finally
        {
            // Cancelled, failed, or too much output: curl must not stay alive
            Kill(process);
        }
    }

    private static async Task WriteConfigAsync(Process process, string config)
    {
        try
        {
            await process.StandardInput.WriteAsync(config);
        }
        catch (IOException)
        {
            // curl stopped reading before the end (for example a wrong option): its exit code says why
        }
        finally
        {
            try
            {
                process.StandardInput.Close();
            }
            catch (IOException)
            {
            }
        }
    }

    private async Task<byte[]> ReadOutputAsync(Process process, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        var stream = process.StandardOutput.BaseStream;

        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > _maxOutputBytes)
            {
                Kill(process);
                throw new HttpRequestException("The answer of Garmin is too large.");
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // it ended in the meantime
        }
    }
}
