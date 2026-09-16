using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration;

public sealed class PipelineOrchestrator
{
    private readonly IEnumerable<IDataQualityStage> _stages;

    public PipelineOrchestrator(IEnumerable<IDataQualityStage> stages)
    {
        _stages = stages;
    }

    public async Task<PipelineRunResult> RunAsync(PipelineContext context, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        bool overallSuccess = true;

        foreach (var stage in _stages)
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
}
