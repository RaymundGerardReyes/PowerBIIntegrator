using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompileTmdlSemanticModel;

public sealed record TmdlCompilationResponse(
    string ModelName,
    int TotalFiles,
    IReadOnlyDictionary<string, string> Files);

public sealed record CompileTmdlSemanticModelCommand(
    Guid AnalyticsModelId) : IRequest<Result<TmdlCompilationResponse>>;

public class CompileTmdlSemanticModelCommandHandler : IRequestHandler<CompileTmdlSemanticModelCommand, Result<TmdlCompilationResponse>>
{
    private readonly ITmdlGenerator _generator;
    private readonly IAnalyticsModelRepository _modelRepository;

    public CompileTmdlSemanticModelCommandHandler(ITmdlGenerator generator, IAnalyticsModelRepository modelRepository)
    {
        _generator = generator;
        _modelRepository = modelRepository;
    }

    public async Task<Result<TmdlCompilationResponse>> Handle(CompileTmdlSemanticModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _modelRepository.GetByIdAsync(request.AnalyticsModelId, cancellationToken);
        model ??= CreateDefaultModel(request.AnalyticsModelId);

        var fileTree = _generator.GenerateSemanticModel(model);
        var files = fileTree.Files.ToDictionary(f => f.RelativePath, f => f.Content);

        return Result<TmdlCompilationResponse>.Success(new TmdlCompilationResponse(model.Name, files.Count, files));
    }

    private static AnalyticsModel CreateDefaultModel(Guid id)
    {
        var model = new AnalyticsModel("SalesAnalyticsModel");
        var table = model.GetOrAddTable("Sales");
        table.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));
        table.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        table.AddColumn(new ModelColumn("Revenue", ColumnDataType.Decimal, "Revenue"));

        var countExpr = new MeasureExpression("COUNTROWS('Sales')", "integer");
        var countMeasure = Measure.Create("TotalRows", countExpr, "Sales");
        if (countMeasure.IsSuccess && countMeasure.Value != null)
        {
            table.AddMeasure(countMeasure.Value);
            model.AddMeasure(countMeasure.Value);
        }

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

