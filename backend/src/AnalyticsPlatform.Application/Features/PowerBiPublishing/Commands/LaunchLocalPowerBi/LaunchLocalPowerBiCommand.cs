using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.LaunchLocalPowerBi;

public sealed record LaunchLocalPowerBiCommand(
    Guid DashboardDefinitionId,
    Guid AnalyticsModelId,
    string? ProjectName = null,
    string? OutputDirectory = null
) : IRequest<Result<LaunchProjectResult>>;

public class LaunchLocalPowerBiCommandHandler : IRequestHandler<LaunchLocalPowerBiCommand, Result<LaunchProjectResult>>
{
    private readonly ILocalPowerBiDesktopService _desktopService;
    private readonly IPbirGenerator _pbirGenerator;
    private readonly ITmdlGenerator _tmdlGenerator;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAnalyticsModelRepository _modelRepository;

    public LaunchLocalPowerBiCommandHandler(
        ILocalPowerBiDesktopService desktopService,
        IPbirGenerator pbirGenerator,
        ITmdlGenerator tmdlGenerator,
        IDashboardRepository dashboardRepository,
        IAnalyticsModelRepository modelRepository)
    {
        _desktopService = desktopService;
        _pbirGenerator = pbirGenerator;
        _tmdlGenerator = tmdlGenerator;
        _dashboardRepository = dashboardRepository;
        _modelRepository = modelRepository;
    }

    public async Task<Result<LaunchProjectResult>> Handle(LaunchLocalPowerBiCommand request, CancellationToken cancellationToken)
    {
        var model = await _modelRepository.GetByIdAsync(request.AnalyticsModelId, cancellationToken);
        model ??= CreateDefaultModel(request.AnalyticsModelId);

        var dashboard = await _dashboardRepository.GetByIdAsync(request.DashboardDefinitionId, cancellationToken);
        if (dashboard == null)
        {
            dashboard = Dashboards.Services.DashboardFactory.CreateFromModel(model, request.DashboardDefinitionId);
        }

        var projectName = !string.IsNullOrWhiteSpace(request.ProjectName)
            ? request.ProjectName
            : (!string.IsNullOrWhiteSpace(model.Name) ? model.Name.Replace(" Semantic Model", "") : dashboard.Name);

        var sanitizedProjectName = SanitizeName(projectName);
        var semanticModelRelativePath = $"../{sanitizedProjectName}.SemanticModel";

        var reportTree = _pbirGenerator.GenerateReportDefinition(dashboard, semanticModelRelativePath);
        var modelTree = _tmdlGenerator.GenerateSemanticModel(model);

        var reportFiles = reportTree.Files.ToDictionary(f => f.RelativePath, f => f.Content);
        var modelFiles = modelTree.Files.ToDictionary(f => f.RelativePath, f => f.Content);

        var result = await _desktopService.LaunchProjectAsync(
            request.OutputDirectory ?? string.Empty,
            sanitizedProjectName,
            reportFiles,
            modelFiles,
            cancellationToken);

        return Result<LaunchProjectResult>.Success(result);
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

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "AnalyticsProject";

        var withoutExt = name;
        foreach (var ext in new[] { ".xls", ".xlsx", ".csv", ".json", ".pbip", ".pbix" })
        {
            if (withoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                withoutExt = withoutExt[..^ext.Length];
                break;
            }
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(withoutExt.Where(c => !invalid.Contains(c) && c != '/' && c != '\\' && c != '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "AnalyticsProject" : cleaned;
    }
}
