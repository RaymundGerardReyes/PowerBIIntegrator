using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Abstractions;

public record PipelineContext(
    string RunId,
    string SourceReference,
    Dictionary<string, object> DataBatches
)
{
    private readonly List<StageRunSummary> _summaries = new();
    public IReadOnlyList<StageRunSummary> Summaries => _summaries;

    public void RecordStage(StageRunSummary summary) => _summaries.Add(summary);
}

public record StageResult(
    string StageName,
    bool IsSuccess,
    bool IsFatal,
    long InputRows,
    long OutputRows,
    long QuarantinedRows,
    IReadOnlyList<string> RulesFired,
    string SummaryDetails
);

public interface IDataQualityStage
{
    string StageName { get; }
    Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct);
}

