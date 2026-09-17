using MediatR;
using AnalyticsPlatform.Application.Features.Dashboards.Queries.GetDashboardDefinition;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Api.Endpoints;

public sealed record CreateDashboardRequest(string Name, Guid AnalyticsModelId);

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboards").WithTags("Dashboards");

        group.MapPost("/", async (CreateDashboardRequest request, IDashboardRepository repo) =>
        {
            var dashboard = new DashboardDefinition(request.Name, request.AnalyticsModelId);
            await repo.AddAsync(dashboard);
            return Results.Ok(new { id = dashboard.Id, name = dashboard.Name });
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetDashboardDefinitionQuery(id));
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Errors);
        });

        group.MapGet("/model/{modelId:guid}", async (Guid modelId, IAnalyticsModelRepository modelRepo) =>
        {
            var model = await modelRepo.GetByIdAsync(modelId);
            if (model == null)
            {
                return Results.NotFound(new { error = $"Model '{modelId}' not found." });
            }

            var dashboard = Application.Features.Dashboards.Services.DashboardFactory.CreateFromModel(model);
            return Results.Ok(new
            {
                id = dashboard.Id.ToString(),
                name = dashboard.Name,
                pages = dashboard.Pages.Select(p => new
                {
                    name = p.Name,
                    canvasWidth = (int)p.CanvasWidth,
                    canvasHeight = (int)p.CanvasHeight,
                    visuals = p.Visuals.Select(v => new
                    {
                        name = v.Name,
                        visualType = v.VisualType,
                        layout = new
                        {
                            x = (int)v.Layout.X,
                            y = (int)v.Layout.Y,
                            width = (int)v.Layout.Width,
                            height = (int)v.Layout.Height,
                            z = v.Layout.ZOrder,
                            visible = v.Layout.Visible
                        },
                        boundFields = v.BoundFields
                    }).ToList()
                }).ToList()
            });
        });
    }
}
