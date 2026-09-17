using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetTransformationPlanTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetTransformationPlanTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_transformation_plan";
    public string Description => "Reads TransformationPlan by ID. Returns step sequence, source and target schema, and step types.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var plan = await _repo.GetTransformationPlanAsync(runOrDatasetId, ct);
        if (plan == null)
        {
            return new AdvisoryToolResult(false, Name, "Transformation plan not found.", Array.Empty<string>(), Array.Empty<string>(), 0);
        }

        var steps = plan.Steps.Select(s => new
        {
            order = s.Order,
            operation = s.OperationType,
            targetColumn = s.TargetColumn,
            sourceOrExpression = s.ExpressionOrSource
        }).ToList();

        var data = new
        {
            planName = plan.PlanName,
            targetGoldTable = plan.TargetGoldTable,
            version = plan.Version,
            stepCount = plan.Steps.Count,
            steps = steps
        };

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: new[] { "TransformationSequencingRule" },
            GroundedRunIds: new[] { plan.PlanName },
            TotalFieldsInspected: plan.Steps.Count * 4 + 4);
    }
}

