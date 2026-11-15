using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using ActivityExporter.Core.Service;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ActivityExporter.Api;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/history");
        group.MapGet("/", List);
        group.MapGet("/{id:int}", Get);
        group.MapDelete("/{id:int}", Delete);
    }

    // The list does not have the coach text: it is written when one activity is opened
    public static async Task<Ok<IReadOnlyList<HistoryActivityResponse>>> List(IActivityRepository repository)
    {
        var activities = await repository.ListAsync();
        var responses = activities.Select(activity => ToResponse(activity, text: null)).ToList();
        return TypedResults.Ok<IReadOnlyList<HistoryActivityResponse>>(responses);
    }

    public static async Task<Results<Ok<HistoryActivityResponse>, NotFound>> Get(int id, ActivityHistoryService history)
    {
        var detail = await history.GetDetailAsync(id);
        if (detail is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToResponse(detail.Activity, detail.Text));
    }

    // A soft delete: the activity leaves the history but the next sync will not bring it back
    public static async Task<Results<NoContent, NotFound>> Delete(int id, IActivityRepository repository)
    {
        if (!await repository.DeleteAsync(id))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static HistoryActivityResponse ToResponse(SavedActivity activity, string? text)
    {
        return new HistoryActivityResponse(
            activity.Id,
            activity.GarminActivityId,
            activity.Name,
            activity.Sport,
            activity.StartTime,
            activity.TotalTime.TotalSeconds,
            activity.DistanceInMeters,
            activity.ElevationGain,
            activity.AvgHR,
            activity.MaxHR,
            activity.Calories,
            activity.Sport.HasCoachText(),
            text);
    }
}
