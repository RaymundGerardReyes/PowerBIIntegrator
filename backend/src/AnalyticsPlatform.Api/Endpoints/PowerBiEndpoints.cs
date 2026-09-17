using MediatR;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbipProject;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompileTmdlSemanticModel;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.ImportPowerBiArtifact;
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

        group.MapGet("/desktop/status", async (ISender sender) =>
        {
            var result = await sender.Send(new Application.Features.PowerBiPublishing.Queries.GetLocalPowerBiStatus.GetLocalPowerBiStatusQuery());
            return Results.Ok(result);
        });

        group.MapPost("/desktop/launch", async (Application.Features.PowerBiPublishing.Commands.LaunchLocalPowerBi.LaunchLocalPowerBiCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess && result.Value != null ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapPost("/desktop/open-folder", async (Application.Features.PowerBiPublishing.Commands.OpenLocalPowerBiFolder.OpenLocalPowerBiFolderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(new { success = result.Value }) : Results.BadRequest(result.Errors);
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

        group.MapPost("/import", async (HttpRequest request, ISender sender, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new[] { "Invalid content type. Expected multipart/form-data." });
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new[] { "File is required." });
            }

            var workspaceId = form["workspaceId"].ToString();
            if (string.IsNullOrWhiteSpace(workspaceId) && request.Query.ContainsKey("workspaceId"))
            {
                workspaceId = request.Query["workspaceId"].ToString();
            }

            var datasetDisplayName = form["datasetDisplayName"].ToString();
            if (string.IsNullOrWhiteSpace(datasetDisplayName) && request.Query.ContainsKey("datasetDisplayName"))
            {
                datasetDisplayName = request.Query["datasetDisplayName"].ToString();
            }

            if (string.IsNullOrWhiteSpace(datasetDisplayName))
            {
                datasetDisplayName = Path.GetFileNameWithoutExtension(file.FileName);
            }

            await using var stream = file.OpenReadStream();
            var command = new ImportPowerBiArtifactCommand(workspaceId, datasetDisplayName, stream, file.FileName);
            var result = await sender.Send(command, ct);
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        }).DisableAntiforgery();
    }
}
