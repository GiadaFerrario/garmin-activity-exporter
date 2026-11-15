using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ActivityExporter.Api;

public static class GarminEndpoints
{
    public static void MapGarminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/garmin");
        // POST and not GET: it changes the history (it saves the new activities)
        group.MapPost("/sync", Sync);
    }

    public static async Task<Results<Ok<GarminSyncResponse>, ProblemHttpResult>> Sync(GarminSyncService service)
    {
        try
        {
            var result = await service.SyncAsync();
            var outcomes = result.Outcomes.Select(ToResponse).ToList();
            return TypedResults.Ok(new GarminSyncResponse(outcomes, result.PostponedCount));
        }
        catch (GarminSourceException exception)
        {
            // Garmin (or the key) is the problem, not our request: 502 Bad Gateway with a message that can be shown
            return TypedResults.Problem(
                title: "Garmin sync failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static GarminSyncOutcomeResponse ToResponse(GarminSyncOutcome outcome)
    {
        return new GarminSyncOutcomeResponse(
            outcome.GarminActivityId,
            outcome.Name,
            outcome.TypeKey,
            outcome.Sport,
            outcome.StartTime,
            outcome.Duration.TotalSeconds,
            outcome.Status,
            outcome.Message,
            outcome.Saved?.Id);
    }
}
