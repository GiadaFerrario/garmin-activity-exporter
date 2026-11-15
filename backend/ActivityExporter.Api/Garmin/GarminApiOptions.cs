namespace ActivityExporter.Api;

/// <summary>
/// Settings of the Garmin Personal API, read from the "Garmin" section of the configuration.
/// The key is a secret: it lives in the user-secrets (or in the environment variable Garmin__ApiKey),
/// never in appsettings.json.
/// </summary>
public class GarminApiOptions
{
    /// <summary>The Personal API key created on Garmin Connect</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://personal.connectapi.garmin.com";

    /// <summary>How many activities to ask for in each request. Garmin failed with an empty 500 on pages of 200.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Whether the requests to Garmin are made by the curl program instead of .NET. Null means "on macOS only".
    /// Why: the Personal API of Garmin accepts only TLS 1.3 and .NET 8 on macOS does not have it
    /// (the handshake fails with "bad protocol version"), while curl has it.
    /// </summary>
    public bool? UseCurl { get; set; }

    /// <summary>The curl program to run: its name if it is in the PATH, or the full path</summary>
    public string CurlPath { get; set; } = "curl";

    public bool ShouldUseCurl(bool isMacOs) => UseCurl ?? isMacOs;
}
