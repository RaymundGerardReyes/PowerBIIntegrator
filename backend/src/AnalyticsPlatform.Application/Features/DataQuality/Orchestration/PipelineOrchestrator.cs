using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration;

public sealed class PipelineOrchestrator
{
    private readonly IEnumerable<IDataQualityStage> _stages;

    public IReadOnlyList<IDataQualityStage> Stages => _stages.ToList();

    public PipelineOrchestrator(IEnumerable<IDataQualityStage> stages)
    {
        _stages = stages;
    }

    public Task<PipelineRunResult> RunAsync(PipelineContext context, CancellationToken ct)
    {
        return RunAsync(context, _stages, ct);
    }

    public async Task<PipelineRunResult> RunAsync(PipelineContext context, IEnumerable<IDataQualityStage> stages, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        bool overallSuccess = true;

        foreach (var stage in stages)
        {
            var res = await stage.ExecuteAsync(context, ct);
            var summary = new StageRunSummary(
                res.StageName,
                res.IsSuccess,
                res.InputRows,
                res.OutputRows,
                res.QuarantinedRows,
                res.RulesFired,
                res.SummaryDetails
            );
            context.RecordStage(summary);

            if (!res.IsSuccess)
                overallSuccess = false;

            if (res.IsFatal)
                break; // Short-circuit pipeline on fatal validation error
        }

        var endTime = DateTime.UtcNow;
        var runResult = new PipelineRunResult(context.RunId, context.SourceReference, startTime, endTime, overallSuccess);
        foreach (var summary in context.Summaries)
        {
            runResult.AddStageSummary(summary);
        }

        return runResult;
    }

    public Task<PipelineRunResult> RunStagesAsync(PipelineContext context, IEnumerable<string> stageNames, CancellationToken ct)
    {
        var stageMap = _stages.ToDictionary(s => s.StageName, StringComparer.OrdinalIgnoreCase);
        var filteredStages = new List<IDataQualityStage>();
        foreach (var name in stageNames)
        {
            if (stageMap.TryGetValue(name, out var stage))
            {
                filteredStages.Add(stage);
            }
        }
        return RunAsync(context, filteredStages, ct);
    }
}

