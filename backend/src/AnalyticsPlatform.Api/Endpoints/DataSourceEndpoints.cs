using MediatR;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Application.Features.DataSources.Queries.GetDataSourceSchema;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Api.Endpoints;

public sealed record UploadDataSourceRequest(string Name, string Type, string ConnectionOrPath);
public sealed record RegisterDataSourceRequest(string Name, string Type, string ConnectionOrPath);
public sealed record RegisterSqlDataSourceRequest(string Name, string ConnectionString, string? Type = null);

public static class DataSourceEndpoints
{
    public static void MapDataSourceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data-sources").WithTags("DataSources");

        group.MapPost("/upload", async (HttpRequest request, ISender sender) =>
        {
            string name;
            DataSourceType type;
            string connectionOrPath;

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new[] { "File is required for upload." });
                }

                var safeFileName = Path.GetFileName(file.FileName);
                var tempDir = Path.Combine(Path.GetTempPath(), "AnalyticsPlatformUploads");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}_{safeFileName}");

                await using (var stream = File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                var typeStr = form["type"].ToString();
                var extension = Path.GetExtension(safeFileName);
                type = Enum.TryParse<DataSourceType>(typeStr, true, out var parsedType)
                    ? parsedType
                    : (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? DataSourceType.Csv : DataSourceType.Excel);

                name = !string.IsNullOrWhiteSpace(form["name"].ToString())
                    ? form["name"].ToString()
                    : safeFileName;

                connectionOrPath = tempPath;
            }
            else
            {
                var body = await request.ReadFromJsonAsync<UploadDataSourceRequest>();
                if (body == null)
                {
                    return Results.BadRequest(new[] { "Invalid upload request payload." });
                }

                name = body.Name;
                type = Enum.TryParse<DataSourceType>(body.Type, true, out var parsedType) ? parsedType : DataSourceType.Excel;
                connectionOrPath = body.ConnectionOrPath;
            }

            var result = await sender.Send(new RegisterDataSourceCommand(name, type, connectionOrPath));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        }).DisableAntiforgery();

        group.MapPost("/register", async (RegisterDataSourceRequest request, ISender sender) =>
        {
            var type = Enum.TryParse<DataSourceType>(request.Type, true, out var parsed) ? parsed : DataSourceType.Excel;
            var result = await sender.Send(new RegisterDataSourceCommand(request.Name, type, request.ConnectionOrPath));
            return result.IsSuccess
                ? Results.Created($"/api/data-sources/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        });

        group.MapPost("/sql", async (RegisterSqlDataSourceRequest request, ISender sender) =>
        {
            var type = !string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<DataSourceType>(request.Type, true, out var parsed)
                ? parsed
                : DataSourceType.SqlServer;

            var result = await sender.Send(new RegisterDataSourceCommand(request.Name, type, request.ConnectionString));
            return result.IsSuccess
                ? Results.Created($"/api/data-sources/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        });

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new AnalyticsPlatform.Application.Features.DataSources.Queries.GetDataSources.GetDataSourcesQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        group.MapGet("/{id:guid}/schema", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetDataSourceSchemaQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
        });
    }
}
