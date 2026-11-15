namespace ActivityExporter.Core.Exceptions;

/// <summary>
/// Garmin could not be reached or refused the request (expired key, server error, bad answer)
/// </summary>
public class GarminSourceException : Exception
{
    public GarminSourceException(string message) : base(message)
    {
    }

    public GarminSourceException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
