using MediatR;
using AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;
using AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;
using AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModel;

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

        group.MapGet("/models/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetAnalyticsModelQuery(id));
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Errors);
        });

        group.MapPost("/models/{id:guid}/validate", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ValidateAnalyticsModelCommand(id));
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });
    }
}
