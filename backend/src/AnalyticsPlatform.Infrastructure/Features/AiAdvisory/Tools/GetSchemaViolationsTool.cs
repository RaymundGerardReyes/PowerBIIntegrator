using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetSchemaViolationsTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetSchemaViolationsTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_schema_violations";
    public string Description => "Reads SchemaCompatibilityRule evaluation results. Returns violated rule, column, expected vs actual type/constraint.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var violations = await _repo.GetSchemaViolationsAsync(runOrDatasetId, ct);
        var ruleIds = violations.Select(v => v.RuleId).Distinct().ToList();

        var data = new
        {
            runId = runOrDatasetId,
            violationCount = violations.Count,
            violations = violations.Select(v => new
            {
                ruleId = v.RuleId,
                ruleName = v.RuleName,
                column = v.ColumnName,
                expected = v.ExpectedType,
                actual = v.ActualType,
                severity = v.Severity
            }).ToList()
        };

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: ruleIds,
            GroundedRunIds: new[] { runOrDatasetId },
            TotalFieldsInspected: violations.Count * 5);
    }
}

