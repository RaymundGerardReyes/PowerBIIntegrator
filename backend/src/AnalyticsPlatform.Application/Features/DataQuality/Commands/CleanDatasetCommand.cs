using System.Globalization;
using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

namespace AnalyticsPlatform.Application.Features.DataQuality.Commands;

public record CleanDatasetCommand(string SourceReference, string DatasetName) : IRequest<Result<PipelineRunResult>>;

public class CleanDatasetCommandHandler : IRequestHandler<CleanDatasetCommand, Result<PipelineRunResult>>
{
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataSourceReaderFactory _readerFactory;
    private readonly PipelineOrchestrator _orchestrator;

    public CleanDatasetCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory)
        : this(dataSourceRepository, readerFactory, null)
    {
    }

    public CleanDatasetCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory,
        PipelineOrchestrator? orchestrator)
    {
        _dataSourceRepository = dataSourceRepository;
        _readerFactory = readerFactory;
        _orchestrator = orchestrator ?? new PipelineOrchestrator(new IDataQualityStage[]
        {
            new ProfilingStage(readerFactory),
            new DeduplicationStage(),
            new CleaningStage()
        });
    }

    public async Task<Result<PipelineRunResult>> Handle(CleanDatasetCommand request, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString();

        var (resolvedPath, dsType, datasetName) = await ResolveDataSourceAsync(request.SourceReference, request.DatasetName, cancellationToken);

        var dataBatches = new Dictionary<string, object>
        {
            ["ResolvedPath"] = resolvedPath,
            ["DataSourceType"] = dsType,
            ["DatasetName"] = datasetName
        };

        if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
        {
            try
            {
                var reader = _readerFactory.GetReader(dsType);
                var rows = await reader.ReadAsync(resolvedPath, cancellationToken);

                if (rows.Count > 0)
                {
                    var headers = rows[0].Keys.ToList();
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
                return Result<PipelineRunResult>.Failure($"Failed to clean dataset: {ex.Message}");
            }
        }
        else
        {
            var emptyBatch = new TabularBatch(datasetName, Array.Empty<string>(), Array.Empty<TabularRow>());
            dataBatches["TabularBatch"] = emptyBatch;
            dataBatches["Batch"] = emptyBatch;
        }

        var context = new PipelineContext(runId, request.SourceReference, dataBatches);

        // Execute stages through PipelineOrchestrator (Profiling, Deduplication, Cleaning)
        var result = await _orchestrator.RunStagesAsync(context, new[] { "Profiling", "Deduplication", "Cleaning" }, cancellationToken);

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

