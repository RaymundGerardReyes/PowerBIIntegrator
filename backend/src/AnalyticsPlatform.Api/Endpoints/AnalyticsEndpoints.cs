using MediatR;
using AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;

namespace AnalyticsPlatform.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapPost("/measures", async (CreateMeasureCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });
    }
}
