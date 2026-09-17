using System.Globalization;
using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.DataQuality.Commands;

public record ProfileDatasetCommand(string SourceReference, string DatasetName) : IRequest<Result<DatasetProfile>>;

public class ProfileDatasetCommandHandler : IRequestHandler<ProfileDatasetCommand, Result<DatasetProfile>>
{
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataSourceReaderFactory _readerFactory;

    public ProfileDatasetCommandHandler(
        IDataSourceRepository dataSourceRepository,
        IDataSourceReaderFactory readerFactory)
    {
        _dataSourceRepository = dataSourceRepository;
        _readerFactory = readerFactory;
    }

    public async Task<Result<DatasetProfile>> Handle(ProfileDatasetCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve Data Source definition or path
        var (resolvedPath, dsType, datasetName) = await ResolveDataSourceAsync(request.SourceReference, request.DatasetName, cancellationToken);

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            // If file does not exist on disk, fallback to an empty dataset profile with 0 rows
            var emptyProfile = new DatasetProfile(datasetName, request.SourceReference, 0);
            return Result.Success(emptyProfile);
        }

        try
        {
            var reader = _readerFactory.GetReader(dsType);
            var rows = await reader.ReadAsync(resolvedPath, cancellationToken);

            var totalRows = rows.Count;
            var profile = new DatasetProfile(datasetName, request.SourceReference, totalRows);

            if (totalRows == 0)
            {
                return Result.Success(profile);
            }

            var headers = rows[0].Keys.ToList();
            foreach (var col in headers)
            {
                var values = rows.Select(r => r.TryGetValue(col, out var v) ? v : null).ToList();
                var nonNullStrings = values
                    .Where(v => v != null && !(v is string s && string.IsNullOrWhiteSpace(s)))
                    .Select(v => v is DateTime dt ? dt.ToString("o", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty)
                    .ToList();

                var nullCount = totalRows - nonNullStrings.Count;
                var nullRatio = (double)nullCount / totalRows;
                var distinctCount = nonNullStrings.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var sampleValues = nonNullStrings.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList();

                var minValue = nonNullStrings.OrderBy(v => v).FirstOrDefault() ?? string.Empty;
                var maxValue = nonNullStrings.OrderByDescending(v => v).FirstOrDefault() ?? string.Empty;

                var inferredType = InferType(nonNullStrings);
                var cardinalityClass = distinctCount <= 5 ? "Low" : (distinctCount <= totalRows * 0.4 ? "Medium" : "High");
                var regexSignature = InferRegex(inferredType);

                profile.AddColumnProfile(new ColumnProfile(
                    col,
                    inferredType,
                    totalRows,
                    nullCount,
                    nullRatio,
                    distinctCount,
                    minValue,
                    maxValue,
                    sampleValues,
                    regexSignature,
                    cardinalityClass));
            }

            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            return Result<DatasetProfile>.Failure($"Failed to profile dataset: {ex.Message}");
        }
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

    private static string InferType(List<string> sampleStrings)
    {
        if (sampleStrings.Count == 0) return "String";
        if (sampleStrings.All(s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))) return "Int64";
        if (sampleStrings.All(s => decimal.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))) return "Decimal";
        if (sampleStrings.All(s => bool.TryParse(s, out _))) return "Boolean";
        if (sampleStrings.All(s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) return "DateTime";
        return "String";
    }

    private static string InferRegex(string inferredType) => inferredType switch
    {
        "Int64" => @"^\d+$",
        "Decimal" => @"^\d+(\.\d+)?$",
        "Boolean" => @"^(true|false|True|False)$",
        "DateTime" => @"^\d{4}-\d{2}-\d{2}",
        _ => @"^.+$"
    };
}

