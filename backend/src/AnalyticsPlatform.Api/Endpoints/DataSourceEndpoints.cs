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

        group.MapPost("/upload", async (UploadDataSourceRequest request, ISender sender) =>
        {
            var type = Enum.TryParse<DataSourceType>(request.Type, true, out var parsed) ? parsed : DataSourceType.Excel;
            var result = await sender.Send(new RegisterDataSourceCommand(request.Name, type, request.ConnectionOrPath));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

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

        group.MapGet("/{id:guid}/schema", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetDataSourceSchemaQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors);
        });
    }
}
