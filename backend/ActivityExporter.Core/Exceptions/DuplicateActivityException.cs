namespace ActivityExporter.Core.Exceptions;

/// <summary>
/// An activity with the same GarminActivityId is already known (saved or deleted)
/// </summary>
public class DuplicateActivityException : Exception
{
    public long GarminActivityId { get; }

    public DuplicateActivityException(long garminActivityId, Exception? innerException = null)
        : base($"An activity with the Garmin id {garminActivityId} is already known.", innerException)
    {
        GarminActivityId = garminActivityId;
    }
}
