using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetPipelineRunResultTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetPipelineRunResultTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_pipeline_run_result";
    public string Description => "Reads PipelineRunResult by run ID. Returns row counts per stage, rules triggered, and quarantine count without raw row values.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var run = await _repo.GetRunResultAsync(runOrDatasetId, ct);
        if (run == null)
        {
            return new AdvisoryToolResult(false, Name, "Pipeline run not found.", Array.Empty<string>(), Array.Empty<string>(), 0);
        }

        var stages = run.StageSummaries.Select(s => new
        {
            stage = s.StageName,
            isSuccess = s.IsSuccess,
            inputRows = s.InputRowCount,
            outputRows = s.OutputRowCount,
            quarantinedRows = s.QuarantinedRowCount,
            rules = s.TriggeredRules,
            details = s.Details
        }).ToList();

        var data = new
        {
            runId = run.RunId,
            source = run.SourceReference,
            isSuccess = run.IsSuccess,
            startedAt = run.StartedAtUtc,
            completedAt = run.CompletedAtUtc,
            totalQuarantined = run.StageSummaries.Sum(s => s.QuarantinedRowCount),
            stages = stages
        };

        var allRules = run.StageSummaries.SelectMany(s => s.TriggeredRules).Distinct().ToList();

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: allRules,
            GroundedRunIds: new[] { run.RunId },
            TotalFieldsInspected: stages.Count * 6 + 5);
    }
}

