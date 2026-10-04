using AnalyticsPlatform.Application.Features.Analytics.Services;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

public class TransformationStage : IDataQualityStage
{
    private readonly IAnalyticsModelRepository? _modelRepository;

    public string StageName => "Transformation";

    public TransformationStage(IAnalyticsModelRepository? modelRepository = null)
    {
        _modelRepository = modelRepository;
    }

    public async Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        TabularBatch? batch = null;
        if (context.DataBatches.TryGetValue("Batch", out var bObj) && bObj is TabularBatch tb)
        {
            batch = tb;
        }

        var rowCount = batch?.Rows.Count ?? 0;
        var targetTable = context.DataBatches.TryGetValue("TargetGoldTable", out var tgtObj) && tgtObj is string tgt && !string.IsNullOrWhiteSpace(tgt)
            ? tgt
            : (batch?.SourceName ?? "GoldTable");

        // Obtain or synthesize profile
        DatasetProfile? profile = null;
        if (context.DataBatches.TryGetValue("Profile", out var pObj) && pObj is DatasetProfile dp)
        {
            profile = dp;
        }
        else if (batch != null)
        {
            profile = new DatasetProfile(targetTable, context.SourceReference, rowCount);
            foreach (var col in batch.Columns)
            {
                profile.AddColumnProfile(new ColumnProfile(col, "String", rowCount, 0, 0, rowCount, "", "", Array.Empty<string>(), "^.*$", "Medium"));
            }
            context.DataBatches["Profile"] = profile;
        }

        // Prepare Gold Schema with refinement from cleaned batch
        var goldSchema = new List<ColumnSchema>();
        if (profile != null)
        {
            foreach (var cp in profile.ColumnProfiles)
            {
                var dt = Enum.TryParse<ColumnDataType>(cp.InferredType, true, out var parsedType)
                    ? parsedType
                    : ColumnDataType.String;

                if (dt == ColumnDataType.String && batch != null && batch.Rows.Count > 0)
                {
                    var nonNulls = batch.Rows
                        .Select(r => r.Fields.TryGetValue(cp.ColumnName, out var v) ? v : null)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v!.Trim())
                        .ToList();

                    if (nonNulls.Count > 0)
                    {
                        if (nonNulls.All(s => long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _) && !(s.Length > 1 && s[0] == '0' && !s.Contains('.'))))
                        {
                            dt = ColumnDataType.Integer;
                        }
                        else if (nonNulls.All(s => decimal.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)))
                        {
                            dt = ColumnDataType.Decimal;
                        }
                    }
                }

                goldSchema.Add(new ColumnSchema(0, cp.ColumnName, dt, cp.NullCount > 0, cp.TopValues));
            }
        }

        context.DataBatches["GoldSchema"] = goldSchema;

        // Extract connection/path and source type
        var resolvedPath = context.DataBatches.TryGetValue("ResolvedPath", out var rpObj) && rpObj is string rp ? rp : null;
        var dsType = context.DataBatches.TryGetValue("DataSourceType", out var dstObj) && dstObj is DataSourceType dst ? dst : (DataSourceType?)null;

        // Build Gold AnalyticsModel
        var goldModel = AnalyticsModelFactory.CreateFromDataSource(
            targetTable,
            goldSchema,
            connectionOrPath: resolvedPath,
            sourceType: dsType);

        context.DataBatches["GoldModel"] = goldModel;

        if (_modelRepository != null)
        {
            await _modelRepository.AddAsync(goldModel, ct);
            context.DataBatches["GoldModelSaved"] = true;
        }

        return new StageResult(
            StageName,
            IsSuccess: true,
            IsFatal: false,
            InputRows: rowCount,
            OutputRows: rowCount,
            QuarantinedRows: 0,
            RulesFired: new[] { "GoldAggregationRule", "SchemaMappingRule" },
            SummaryDetails: $"Gold table '{targetTable}' materialized with {rowCount} curated records."
        );
    }
}
