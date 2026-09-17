using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

public interface IAdvisoryRunRepository
{
    Task<PipelineRunResult?> GetRunResultAsync(string runId, CancellationToken ct = default);
    Task<DatasetProfile?> GetDatasetProfileAsync(string datasetId, CancellationToken ct = default);
    Task<IReadOnlyList<DuplicateCluster>> GetDuplicateClustersAsync(string runId, CancellationToken ct = default);
    Task<IReadOnlyList<SchemaViolationSummary>> GetSchemaViolationsAsync(string runId, CancellationToken ct = default);
    Task<TransformationPlan?> GetTransformationPlanAsync(string planId, CancellationToken ct = default);
    Task<IReadOnlyList<ChartSuggestionSummary>> GetChartSuggestionsAsync(string runId, CancellationToken ct = default);
    Task SaveRunResultAsync(
        PipelineRunResult result,
        DatasetProfile? profile = null,
        IReadOnlyList<DuplicateCluster>? clusters = null,
        IReadOnlyList<ChartSuggestionSummary>? chartSuggestions = null,
        CancellationToken ct = default);
}

