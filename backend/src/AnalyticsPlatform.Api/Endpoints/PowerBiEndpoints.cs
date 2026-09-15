using MediatR;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbipProject;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompileTmdlSemanticModel;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.DownloadPbipPackage;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.GetReportEmbedConfig;

namespace AnalyticsPlatform.Api.Endpoints;

public static class PowerBiEndpoints
{
    public static void MapPowerBiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/powerbi").WithTags("PowerBi");

        group.MapPost("/publish", async (PublishPbipToFabricCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        });

        group.MapGet("/embed-config/{reportId}", async (string reportId, ISender sender) =>
        {
            var result = await sender.Send(new GetReportEmbedConfigQuery(reportId));
            return Results.Ok(result);
        });

        group.MapPost("/compile-pbip", async (CompilePbipProjectCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/compile-pbip/download", async (DownloadPbipPackageQuery query, ISender sender) =>
        {
            var result = await sender.Send(query);
            return result.IsSuccess && result.Value != null
                ? Results.File(result.Value.ZipBytes, result.Value.ContentType, result.Value.FileName)
                : Results.BadRequest(result.Errors);
        });

        group.MapPost("/compile-pbir", async (CompilePbirDefinitionCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/compile-tmdl", async (CompileTmdlSemanticModelCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });
    }
}
