using System.Globalization;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

public class ProfilingStage : IDataQualityStage
{
    private readonly IDataSourceReaderFactory? _readerFactory;

    public string StageName => "Profiling";

    public ProfilingStage(IDataSourceReaderFactory? readerFactory = null)
    {
        _readerFactory = readerFactory;
    }

    public async Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        TabularBatch? batch = null;
        if (context.DataBatches.TryGetValue("Batch", out var bObj) && bObj is TabularBatch tb)
        {
            batch = tb;
        }
        else if (context.DataBatches.TryGetValue("TabularBatch", out var tbObj) && tbObj is TabularBatch tBatch)
        {
            batch = tBatch;
        }

        var datasetName = context.DataBatches.TryGetValue("DatasetName", out var dnObj) && dnObj is string dn
            ? dn
            : Path.GetFileNameWithoutExtension(context.SourceReference);

        var filePath = context.DataBatches.TryGetValue("ResolvedPath", out var rpObj) && rpObj is string rp && File.Exists(rp)
            ? rp
            : (File.Exists(context.SourceReference) ? context.SourceReference : null);

        if (batch == null && _readerFactory != null && !string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                var dsType = context.DataBatches.TryGetValue("DataSourceType", out var dstObj) && dstObj is DataSourceType dst
                    ? dst
                    : (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? DataSourceType.Csv : DataSourceType.Excel);
                var reader = _readerFactory.GetReader(dsType);
                var rows = await reader.ReadAsync(filePath, ct);
                var headers = rows.Count > 0 ? rows[0].Keys.ToList() : new List<string>();
                var tabularRows = rows.Select((r, idx) => new TabularRow(
                    $"row_{idx}",
                    r.ToDictionary(k => k.Key, v => v.Value?.ToString())
                )).ToList();
                batch = new TabularBatch(datasetName, headers, tabularRows);
                context.DataBatches["TabularBatch"] = batch;
                context.DataBatches["Batch"] = batch;
            }
            catch
            {
                // Proceed with empty batch if read fails
            }
        }

        var totalRows = batch?.Rows.Count ?? 0;
        var profile = new DatasetProfile(datasetName, context.SourceReference, totalRows);

        if (batch != null && batch.Rows.Count > 0 && batch.Columns.Count > 0)
        {
            foreach (var col in batch.Columns)
            {
                var values = batch.Rows.Select(r => r.Fields.TryGetValue(col, out var v) ? v : null).ToList();
                var nonNullStrings = values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!.Trim())
                    .ToList();

                var nullCount = totalRows - nonNullStrings.Count;
                var nullRatio = totalRows > 0 ? (double)nullCount / totalRows : 0.0;
                var distinctCount = nonNullStrings.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var sampleValues = nonNullStrings.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList();

                var minValue = nonNullStrings.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;
                var maxValue = nonNullStrings.OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;

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
        }

        context.DataBatches["Profile"] = profile;

        var details = totalRows > 0
            ? $"Profiled {totalRows} rows across {batch?.Columns.Count ?? 0} columns."
            : "Profiled empty dataset.";

        return new StageResult(
            StageName,
            IsSuccess: true,
            IsFatal: false,
            InputRows: totalRows,
            OutputRows: totalRows,
            QuarantinedRows: 0,
            RulesFired: new[] { "ProfileEngine" },
            SummaryDetails: details
        );
    }

    private static readonly System.Text.RegularExpressions.Regex CurrencySymbolRegex = new(@"^[₱$€£¥]\s*|\s*[₱$€£¥]$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string InferType(List<string> sampleStrings)
    {
        if (sampleStrings.Count == 0) return "String";

        // Whole numbers without leading zeros (leading zero IDs like "001" stay Text as per Power BI guidelines)
        if (sampleStrings.All(s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) && !(s.Length > 1 && s[0] == '0' && !s.Contains('.'))))
            return "Int64";

        // Decimals, currency strings (e.g. ₱1,500, $2,000) and thousands-separated numbers
        if (sampleStrings.All(s =>
        {
            var clean = CurrencySymbolRegex.Replace(s.Trim(), "").Replace(",", "", StringComparison.Ordinal).Trim();
            if (clean.StartsWith('(') && clean.EndsWith(')') && clean.Length > 2) clean = clean[1..^1].Trim();
            return decimal.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }))
            return "Decimal";

        // Boolean
        if (sampleStrings.All(s => bool.TryParse(s, out _)))
            return "Boolean";

        // DateTime
        if (sampleStrings.All(s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out _)))
            return "DateTime";

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
