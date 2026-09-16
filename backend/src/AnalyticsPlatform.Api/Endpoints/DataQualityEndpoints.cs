using MediatR;
using AnalyticsPlatform.Application.Features.DataQuality.Commands;
using AnalyticsPlatform.Application.Features.DataQuality.Queries;

namespace AnalyticsPlatform.Api.Endpoints;

public sealed record ProfileRequest(string SourceReference, string DatasetName);
public sealed record CleanRequest(string SourceReference, string DatasetName);
public sealed record TransformRequest(string SilverSourceTable, string TransformationPlanName, string TargetGoldTable);
public sealed record FullPipelineRequest(string SourceReference, string DatasetName, string TargetGoldTable);

public static class DataQualityEndpoints
{
    public static void MapDataQualityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data-quality").WithTags("DataQuality");

        group.MapPost("/profile", async (ProfileRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ProfileDatasetCommand(request.SourceReference, request.DatasetName));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/clean", async (CleanRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CleanDatasetCommand(request.SourceReference, request.DatasetName));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/transform", async (TransformRequest request, ISender sender) =>
        {
            var result = await sender.Send(new TransformDatasetCommand(request.SilverSourceTable, request.TransformationPlanName, request.TargetGoldTable));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/run-full-pipeline", async (FullPipelineRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RunFullPipelineCommand(request.SourceReference, request.DatasetName, request.TargetGoldTable));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapGet("/chart-suggestions/{tableId}", async (string tableId, ISender sender) =>
        {
            var result = await sender.Send(new SuggestChartsForTableQuery(tableId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
        });
    }
}

