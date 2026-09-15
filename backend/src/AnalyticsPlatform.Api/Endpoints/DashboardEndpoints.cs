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

        group.MapGet("/{id:guid}", async (Guid id, IDashboardRepository repo) =>
        {
            var dashboard = await repo.GetByIdAsync(id);
            return dashboard != null ? Results.Ok(dashboard) : Results.NotFound();
        });
    }
}
