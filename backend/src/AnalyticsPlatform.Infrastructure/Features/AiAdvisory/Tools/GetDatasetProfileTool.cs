using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;

public sealed class GetDatasetProfileTool : IAdvisoryTool
{
    private readonly IAdvisoryRunRepository _repo;

    public GetDatasetProfileTool(IAdvisoryRunRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public string Name => "get_dataset_profile";
    public string Description => "Reads DatasetProfile by dataset ID. Returns column profiles (types, null %, cardinality, detected patterns) with zero raw row values.";

    public async Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct)
    {
        var profile = await _repo.GetDatasetProfileAsync(runOrDatasetId, ct);
        if (profile == null)
        {
            return new AdvisoryToolResult(false, Name, "Dataset profile not found.", Array.Empty<string>(), Array.Empty<string>(), 0);
        }

        var columnSummaries = profile.ColumnProfiles.Select(c => new
        {
            column = c.ColumnName,
            inferredType = c.InferredType,
            nullCount = c.NullCount,
            nullRatio = c.NullRatio,
            distinctCount = c.DistinctCount,
            cardinalityClass = c.CardinalityClass,
            detectedPattern = c.DetectedPatternRegex
        }).ToList();

        var data = new
        {
            datasetName = profile.DatasetName,
            source = profile.SourceReference,
            totalRows = profile.TotalRows,
            columnCount = profile.ColumnProfiles.Count,
            columns = columnSummaries
        };

        return new AdvisoryToolResult(
            Success: true,
            ToolName: Name,
            Data: data,
            GroundedRuleIds: new[] { "StatisticalProfileRule" },
            GroundedRunIds: new[] { profile.DatasetName },
            TotalFieldsInspected: columnSummaries.Count * 6 + 4);
    }
}

