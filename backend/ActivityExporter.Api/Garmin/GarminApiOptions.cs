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
}
