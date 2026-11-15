using System.Text.Json;
using ActivityExporter.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace ActivityExporter.Api;

/// <summary>
/// Exchanges the Personal API key for the access token of Garmin and keeps it until it is about to expire.
/// The token lasts 24 hours and there is no refresh token: when it expires the key is exchanged again.
/// </summary>
public class GarminTokenProvider
{
    private const string TokenPath = "/personalapitoken-service/personal-api/personal-api-key/token";

    // Renewed a little early, so that a token does not expire in the middle of a sync
    private static readonly TimeSpan RenewBeforeExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LifetimeWhenExpiryIsUnknown = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly GarminApiOptions _options;
    private readonly TimeProvider _time;

    // One exchange at a time: two requests that need a token together must not ask Garmin twice
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;

    public GarminTokenProvider(HttpClient http, IOptions<GarminApiOptions> options, TimeProvider time)
    {
        _http = http;
        _options = options.Value;
        _time = time;
    }

    /// <exception cref="GarminSourceException">When the key is missing, refused, or Garmin cannot be reached</exception>
    public async Task<string> GetTokenAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_token is not null && _time.GetUtcNow() < _validUntil)
            {
                return _token;
            }

            var token = await ExchangeKeyAsync();
            _token = token;
            _validUntil = ValidUntil(token);
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Forgets the token, for example because Garmin refused it before its time (the key was deleted)
    /// </summary>
    public void Invalidate()
    {
        _gate.Wait();
        try
        {
            _token = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ExchangeKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new GarminSourceException("The Garmin API key is not configured: set Garmin:ApiKey in the user-secrets.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenPath);
        // Not the usual "Bearer": Garmin wants the word ApiKey before the key
        request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {_options.ApiKey}");

        try
        {
            using var response = await _http.SendAsync(request);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                throw new GarminSourceException(
                    "Garmin refused the API key: it may be expired or deleted. Create a new one on Garmin Connect.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new GarminSourceException($"Garmin refused the token request (HTTP {(int)response.StatusCode}).");
            }

            // The answer is the token itself as plain text (a JWT), not a JSON object
            var token = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
            if (token.Length == 0)
            {
                throw new GarminSourceException("Garmin sent an empty token.");
            }

            return token;
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

    private DateTimeOffset ValidUntil(string token)
    {
        return TryReadExpiry(token, out var expiry)
            ? expiry - RenewBeforeExpiry
            : _time.GetUtcNow() + LifetimeWhenExpiryIsUnknown;
    }

    // A JWT has three parts separated by dots; the second one is a JSON (in base64url) with the claim "exp"
    private static bool TryReadExpiry(string token, out DateTimeOffset expiry)
    {
        expiry = default;
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("exp", out var exp)
                && exp.TryGetInt64(out var seconds))
            {
                expiry = DateTimeOffset.FromUnixTimeSeconds(seconds);
                return true;
            }
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            // not a readable token: the caller uses a short lifetime
        }

        return false;
    }
}
