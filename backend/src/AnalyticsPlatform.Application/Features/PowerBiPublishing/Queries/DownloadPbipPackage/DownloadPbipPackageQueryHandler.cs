using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.DownloadPbipPackage;

public class DownloadPbipPackageQueryHandler : IRequestHandler<DownloadPbipPackageQuery, Result<PbipPackageDownloadResponse>>
{
    private readonly IPbipCompiler _compiler;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAnalyticsModelRepository _modelRepository;

    public DownloadPbipPackageQueryHandler(
        IPbipCompiler compiler,
        IDashboardRepository dashboardRepository,
        IAnalyticsModelRepository modelRepository)
    {
        _compiler = compiler;
        _dashboardRepository = dashboardRepository;
        _modelRepository = modelRepository;
    }

    public async Task<Result<PbipPackageDownloadResponse>> Handle(DownloadPbipPackageQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(request.DashboardDefinitionId, cancellationToken);
        var model = await _modelRepository.GetByIdAsync(request.AnalyticsModelId, cancellationToken);
        model ??= CreateDefaultModel(request.AnalyticsModelId);
        dashboard ??= Dashboards.Services.DashboardFactory.CreateFromModel(model, request.DashboardDefinitionId);

        var projectName = !string.IsNullOrWhiteSpace(request.ProjectName)
            ? request.ProjectName
            : dashboard.Name;

        var fileTree = _compiler.CompileProject(projectName, dashboard, model);
        var zipBytes = fileTree.ToZipArchive();

        var fileName = $"{projectName}.pbip.zip";
        var response = new PbipPackageDownloadResponse(fileName, zipBytes, "application/zip");
        return Result<PbipPackageDownloadResponse>.Success(response);
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

    private static AnalyticsModel CreateDefaultModel(Guid id)
    {
        var model = new AnalyticsModel("SalesAnalyticsModel");
        var table = model.GetOrAddTable("Sales");
        table.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));
        table.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        table.AddColumn(new ModelColumn("Revenue", ColumnDataType.Decimal, "Revenue"));

        var measureExpr = new MeasureExpression("SUM(Sales[Revenue])", "decimal");
        var measureResult = Measure.Create("TotalRevenue", measureExpr, "Sales");
        if (measureResult.IsSuccess && measureResult.Value != null)
        {
            table.AddMeasure(measureResult.Value);
            model.AddMeasure(measureResult.Value);
        }

        return model;
    }
}

