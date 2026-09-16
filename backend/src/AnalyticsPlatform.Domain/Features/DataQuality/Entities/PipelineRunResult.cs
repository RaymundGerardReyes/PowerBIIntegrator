using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public record StageRunSummary(
    string StageName,
    bool IsSuccess,
    long InputRowCount,
    long OutputRowCount,
    long QuarantinedRowCount,
    IReadOnlyList<string> TriggeredRules,
    string Details
);

public class PipelineRunResult : Entity
{
    public string RunId { get; private set; }
    public string SourceReference { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
    public bool IsSuccess { get; private set; }
    public List<StageRunSummary> StageSummaries { get; } = new();

    public PipelineRunResult(string runId, string sourceReference, DateTime startedAtUtc, DateTime completedAtUtc, bool isSuccess)
    {
        RunId = runId;
        SourceReference = sourceReference;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        IsSuccess = isSuccess;
    }

    public void AddStageSummary(StageRunSummary summary)
    {
        StageSummaries.Add(summary);
    }
}

