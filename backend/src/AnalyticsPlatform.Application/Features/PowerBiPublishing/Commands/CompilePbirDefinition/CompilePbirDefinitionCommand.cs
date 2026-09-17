using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;

public sealed record PbirCompilationResponse(
    string DashboardName,
    int TotalFiles,
    IReadOnlyDictionary<string, string> Files);

public sealed record CompilePbirDefinitionCommand(
    Guid DashboardDefinitionId,
    string? SemanticModelRelativePath = null,
    Guid? AnalyticsModelId = null) : IRequest<Result<PbirCompilationResponse>>;

public class CompilePbirDefinitionCommandHandler : IRequestHandler<CompilePbirDefinitionCommand, Result<PbirCompilationResponse>>
{
    private readonly IPbirGenerator _generator;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAnalyticsModelRepository? _modelRepository;

    public CompilePbirDefinitionCommandHandler(
        IPbirGenerator generator,
        IDashboardRepository dashboardRepository)
        : this(generator, dashboardRepository, null)
    {
    }

    public CompilePbirDefinitionCommandHandler(
        IPbirGenerator generator,
        IDashboardRepository dashboardRepository,
        IAnalyticsModelRepository? modelRepository)
    {
        _generator = generator;
        _dashboardRepository = dashboardRepository;
        _modelRepository = modelRepository;
    }

    public async Task<Result<PbirCompilationResponse>> Handle(CompilePbirDefinitionCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(request.DashboardDefinitionId, cancellationToken);
        if (dashboard == null)
        {
            if (request.AnalyticsModelId.HasValue && _modelRepository != null)
            {
                var model = await _modelRepository.GetByIdAsync(request.AnalyticsModelId.Value, cancellationToken);
                if (model != null)
                {
                    dashboard = Dashboards.Services.DashboardFactory.CreateFromModel(model, request.DashboardDefinitionId);
                }
            }

            dashboard ??= CreateDefaultDashboard(request.DashboardDefinitionId);
        }

        var modelPath = !string.IsNullOrWhiteSpace(request.SemanticModelRelativePath)
            ? request.SemanticModelRelativePath
            : "../SemanticModel";

        var fileTree = _generator.GenerateReportDefinition(dashboard, modelPath);
        var files = fileTree.Files.ToDictionary(f => f.RelativePath, f => f.Content);

        return Result<PbirCompilationResponse>.Success(new PbirCompilationResponse(dashboard.Name, files.Count, files));
    }

    private static DashboardDefinition CreateDefaultDashboard(Guid id)
    {
        var dashboard = new DashboardDefinition("ExecutiveAnalytics", id);
        var page = new Page("Overview", 1280, 720);
        page.AddVisual(new Visual(
            VisualTypes.BarChart,
            "SalesByRegionBarChart",
            new VisualLayout(20, 20, 600, 340, 1, true),
            ["Sales[Region]", "Sales[TotalRevenue]"]));
        dashboard.AddPage(page);
        return dashboard;
    }
}

