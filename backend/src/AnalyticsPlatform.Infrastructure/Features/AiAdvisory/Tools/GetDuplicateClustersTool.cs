using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetDuplicateClustersTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetDuplicateClustersTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_duplicate_clusters";
    public string Description => "Reads DuplicateClusters by run ID. Returns cluster summaries: rule fired, similarity score, and row ID counts without raw PII values.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var clusters = await _repo.GetDuplicateClustersAsync(runOrDatasetId, ct);
        var ruleIds = clusters.Select(c => c.RuleFired).Distinct().ToList();

        var summaries = clusters.Select(c => new
        {
            clusterId = c.ClusterId,
            ruleFired = c.RuleFired,
            keptRowId = c.KeptRowId,
            droppedRowCount = c.DroppedRowIds.Count,
            confidenceScore = c.ConfidenceScore,
            reasonCode = c.ReasonCode
        }).ToList();

        var data = new
        {
            runId = runOrDatasetId,
            totalClusters = clusters.Count,
            totalDroppedRows = clusters.Sum(c => c.DroppedRowIds.Count),
            clusters = summaries
        };

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: ruleIds,
            GroundedRunIds: new[] { runOrDatasetId },
            TotalFieldsInspected: clusters.Count * 6 + 3);
    }
}

