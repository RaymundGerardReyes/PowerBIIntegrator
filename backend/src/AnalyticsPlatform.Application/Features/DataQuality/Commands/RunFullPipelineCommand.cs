using System.Globalization;
using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Rules;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

namespace AnalyticsPlatform.Application.Features.DataQuality.Commands;

public record RunFullPipelineCommand(string SourceReference, string DatasetName, string TargetGoldTable) : IRequest<Result<PipelineRunResult>>;

public class RunFullPipelineCommandHandler : IRequestHandler<RunFullPipelineCommand, Result<PipelineRunResult>>
{
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataSourceReaderFactory _readerFactory;
    private readonly IAdvisoryRunRepository _advisoryRunRepository;
    private readonly ISender _sender;
    private readonly IAnalyticsModelRepository? _modelRepository;
    private readonly PipelineOrchestrator _orchestrator;

    public RunFullPipelineCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory,
        IAdvisoryRunRepository advisoryRunRepository,
        ISender sender)
        : this(dataSourceRepository, readerFactory, advisoryRunRepository, sender, null, null)
    {
    }

    public RunFullPipelineCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory,
        IAdvisoryRunRepository advisoryRunRepository,
        ISender sender,
        IAnalyticsModelRepository? modelRepository)
        : this(dataSourceRepository, readerFactory, advisoryRunRepository, sender, modelRepository, null)
    {
    }

    public RunFullPipelineCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory,
        IAdvisoryRunRepository advisoryRunRepository,
        ISender sender,
        IAnalyticsModelRepository? modelRepository,
        PipelineOrchestrator? orchestrator)
    {
        _dataSourceRepository = dataSourceRepository;
        _readerFactory = readerFactory;
        _advisoryRunRepository = advisoryRunRepository;
        _sender = sender;
        _modelRepository = modelRepository;
        _orchestrator = orchestrator ?? new PipelineOrchestrator(new IDataQualityStage[]
        {
            new ProfilingStage(readerFactory),
            new DeduplicationStage(),
            new CleaningStage(),
            new TransformationStage(modelRepository)
        });
    }

    public async Task<Result<PipelineRunResult>> Handle(RunFullPipelineCommand request, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString();

        // 1. Resolve Data Source definition or path
        var (resolvedPath, dsType, datasetName) = await ResolveDataSourceAsync(request.SourceReference, request.DatasetName, cancellationToken);

        var dataBatches = new Dictionary<string, object>
        {
            ["ResolvedPath"] = resolvedPath,
            ["DataSourceType"] = dsType,
            ["DatasetName"] = datasetName,
            ["TargetGoldTable"] = request.TargetGoldTable
        };

        int totalRawRows = 0;
        var headers = new List<string>();

        if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
        {
            try
            {
                var reader = _readerFactory.GetReader(dsType);
                var rows = await reader.ReadAsync(resolvedPath, cancellationToken);
                totalRawRows = rows.Count;

                if (totalRawRows > 0)
                {
                    headers = rows[0].Keys.ToList();
                    var tabularRows = rows.Select((r, idx) => new TabularRow(
                        $"row_{idx}",
                        r.ToDictionary(k => k.Key, v => v.Value?.ToString())
                    )).ToList();
                    var rawBatch = new TabularBatch(datasetName, headers, tabularRows);
                    dataBatches["TabularBatch"] = rawBatch;
                    dataBatches["Batch"] = rawBatch;
                }
                else
                {
                    var emptyBatch = new TabularBatch(datasetName, Array.Empty<string>(), Array.Empty<TabularRow>());
                    dataBatches["TabularBatch"] = emptyBatch;
                    dataBatches["Batch"] = emptyBatch;
                }
            }
            catch (Exception ex)
            {
                return Result<PipelineRunResult>.Failure($"Failed to execute pipeline on dataset: {ex.Message}");
            }
        }
        else
        {
            var emptyBatch = new TabularBatch(datasetName, Array.Empty<string>(), Array.Empty<TabularRow>());
            dataBatches["TabularBatch"] = emptyBatch;
            dataBatches["Batch"] = emptyBatch;
        }

        var context = new PipelineContext(runId, request.SourceReference, dataBatches);

        // 2. Execute concrete stages through PipelineOrchestrator
        var result = await _orchestrator.RunAsync(context, cancellationToken);

        // 3. Extract profile and duplicate clusters generated by concrete stages
        DatasetProfile? profile = null;
        if (context.DataBatches.TryGetValue("Profile", out var pObj) && pObj is DatasetProfile dp)
        {
            profile = dp;
        }
        else
        {
            profile = new DatasetProfile(datasetName, request.SourceReference, totalRawRows);
        }

        var duplicateClusters = context.DataBatches.TryGetValue("DuplicateClusters", out var dcObj) && dcObj is IReadOnlyList<DuplicateCluster> dcList
            ? dcList
            : Array.Empty<DuplicateCluster>();

        var chartSuggestions = VisualMappingRule.MapSuggestions(profile);

        // 4. Synthesize Gold model with actual Power Query M partition embedding
        if (_modelRepository != null && !context.DataBatches.ContainsKey("GoldModelSaved"))
        {
            var goldSchema = profile?.ColumnProfiles.Select(cp => new AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnSchema(
                0,
                cp.ColumnName,
                Enum.TryParse<AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType>(cp.InferredType, true, out var dt) ? dt : AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType.String,
                cp.NullCount > 0,
                cp.TopValues
            )).ToList() ?? new List<AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnSchema>();

            var goldModel = AnalyticsPlatform.Application.Features.Analytics.Services.AnalyticsModelFactory.CreateFromDataSource(
                request.TargetGoldTable,
                goldSchema,
                connectionOrPath: resolvedPath,
                sourceType: dsType);

            await _modelRepository.AddAsync(goldModel, cancellationToken);
            context.DataBatches["GoldModel"] = goldModel;
            context.DataBatches["GoldModelSaved"] = true;
        }

        var distinctCount = result.StageSummaries.FirstOrDefault(s => s.StageName == "Deduplication")?.OutputRowCount
            ?? (long)totalRawRows;

        var chartNames = chartSuggestions.Select(s => s.RecommendedVisualType).ToList();
        result.AddStageSummary(new StageRunSummary(
            "ChartSuggestion",
            true,
            distinctCount,
            distinctCount,
            0,
            new[] { "VisualMappingRule" },
            $"Generated {chartSuggestions.Count} visual recommendations: {string.Join(", ", chartNames)}."));

        var chartSummaries = chartSuggestions.Select(s => new ChartSuggestionSummary(
            $"VisualRule-{s.RecommendedVisualType}",
            s.RecommendedVisualType,
            headers.FirstOrDefault(h => h.Contains("Revenue", StringComparison.OrdinalIgnoreCase) || h.Contains("Cost", StringComparison.OrdinalIgnoreCase) || h.Contains("Amount", StringComparison.OrdinalIgnoreCase) || h.Contains("Price", StringComparison.OrdinalIgnoreCase) || h.Contains("Total", StringComparison.OrdinalIgnoreCase)) ?? headers.LastOrDefault() ?? "Metric",
            headers.FirstOrDefault(h => h.Contains("Category", StringComparison.OrdinalIgnoreCase) || h.Contains("Region", StringComparison.OrdinalIgnoreCase) || h.Contains("Date", StringComparison.OrdinalIgnoreCase) || h.Contains("Name", StringComparison.OrdinalIgnoreCase) || h.Contains("Segment", StringComparison.OrdinalIgnoreCase)) ?? headers.FirstOrDefault() ?? "Dimension",
            s.Reason
        )).ToList();

        await _advisoryRunRepository.SaveRunResultAsync(result, profile, duplicateClusters, chartSummaries, cancellationToken);

        return Result.Success(result);
    }

    private async Task<(string ResolvedPath, DataSourceType Type, string Name)> ResolveDataSourceAsync(
        string sourceRef,
        string datasetName,
        CancellationToken ct)
    {
        if (Guid.TryParse(sourceRef, out var dsId))
        {
            var ds = await _dataSourceRepository.GetByIdAsync(dsId, ct);
            if (ds != null) return (ds.ConnectionOrPath, ds.Type, ds.Name);
        }

        var match = await _dataSourceRepository.GetByNameOrPathAsync(sourceRef, ct);
        if (match != null)
        {
            return (match.ConnectionOrPath, match.Type, match.Name);
        }

        if (File.Exists(sourceRef))
        {
            var ext = Path.GetExtension(sourceRef).ToLowerInvariant();
            var type = ext == ".csv" ? DataSourceType.Csv : DataSourceType.Excel;
            return (sourceRef, type, string.IsNullOrWhiteSpace(datasetName) ? Path.GetFileNameWithoutExtension(sourceRef) : datasetName);
        }

        var all = await _dataSourceRepository.GetAllAsync(ct);
        if (all.Count > 0)
        {
            var latest = all[^1];
            return (latest.ConnectionOrPath, latest.Type, latest.Name);
        }

        return (sourceRef, DataSourceType.Csv, datasetName);
    }
}


