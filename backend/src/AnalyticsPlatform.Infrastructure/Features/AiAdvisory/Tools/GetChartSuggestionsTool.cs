using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetChartSuggestionsTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetChartSuggestionsTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_chart_suggestions";
    public string Description => "Reads VisualMappingRule outputs for a table. Returns ranked visual suggestions and rule-based reasons.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var suggestions = await _repo.GetChartSuggestionsAsync(runOrDatasetId, ct);
        var ruleIds = suggestions.Select(s => s.RuleId).Distinct().ToList();

        var data = new
        {
            runId = runOrDatasetId,
            suggestionCount = suggestions.Count,
            suggestions = suggestions.Select(s => new
            {
                ruleId = s.RuleId,
                chartType = s.ChartType,
                measure = s.SuggestedMeasure,
                category = s.CategoryColumn,
                reason = s.Reason
            }).ToList()
        };

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: ruleIds,
            GroundedRunIds: new[] { runOrDatasetId },
            TotalFieldsInspected: suggestions.Count * 4);
    }
}

