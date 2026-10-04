using System.Globalization;
using System.Text.RegularExpressions;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

public class CleaningStage : IDataQualityStage
{
    public string StageName => "Cleaning";

    private static readonly HashSet<string> MissingValuePlaceholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "null", "na", "n/a", "#n/a", "n.a.", "-", "--", "unknown", "none", "(blank)", "(null)", "not available"
    };

    private static readonly Regex CurrencyRegex = new(@"^[₱$€£¥]\s*|\s*[₱$€£¥]$", RegexOptions.Compiled);

    public Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        TabularBatch? batch = null;
        if (context.DataBatches.TryGetValue("Batch", out var bObj) && bObj is TabularBatch tb)
        {
            batch = tb;
        }
        else if (context.DataBatches.TryGetValue("CleanBatch", out var cbObj) && cbObj is TabularBatch cb)
        {
            batch = cb;
        }

        var inputRows = batch?.Rows.Count ?? 0;
        if (batch == null || inputRows == 0)
        {
            return Task.FromResult(new StageResult(
                StageName,
                IsSuccess: true,
                IsFatal: false,
                InputRows: 0,
                OutputRows: 0,
                QuarantinedRows: 0,
                RulesFired: new[] { "TrimWhitespace", "NullStandardization" },
                SummaryDetails: "Standardized 0 Silver rows."
            ));
        }

        var valueMappings = context.DataBatches.TryGetValue("ValueMappings", out var vmObj) && vmObj is IDictionary<string, IDictionary<string, string>> vm
            ? vm
            : null;

        var cleanedRows = new List<TabularRow>(batch.Rows.Count);
        foreach (var row in batch.Rows)
        {
            var cleanedFields = new Dictionary<string, string?>(row.Fields.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (col, val) in row.Fields)
            {
                if (val == null)
                {
                    cleanedFields[col] = null;
                    continue;
                }

                // 1. Clean control characters & trim whitespace (Power Query Text.Clean & Text.Trim)
                var rawChars = val.Where(c => !char.IsControl(c) || c == ' ' || c == '\t').ToArray();
                var trimmed = new string(rawChars).Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    cleanedFields[col] = null;
                    continue;
                }

                // 2. Handle missing value placeholders
                if (MissingValuePlaceholders.Contains(trimmed))
                {
                    cleanedFields[col] = null;
                    continue;
                }

                // 3. Optional Categorical value mapping (e.g. ACTIVE -> Active)
                if (valueMappings != null && valueMappings.TryGetValue(col, out var colMapping) && colMapping.TryGetValue(trimmed, out var mappedVal))
                {
                    cleanedFields[col] = mappedVal;
                    continue;
                }

                // 4. Currency and number cleaning (e.g. ₱1,500, $2,000, 1500 €, (₱500))
                var toProcess = trimmed;
                bool isNegative = false;
                if (toProcess.StartsWith('(') && toProcess.EndsWith(')') && toProcess.Length > 2)
                {
                    isNegative = true;
                    toProcess = toProcess[1..^1].Trim();
                }

                if (CurrencyRegex.IsMatch(toProcess))
                {
                    var withoutSymbol = CurrencyRegex.Replace(toProcess, "").Trim();
                    var withoutCommas = withoutSymbol.Replace(",", "");
                    if (decimal.TryParse(withoutCommas, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedNum))
                    {
                        var finalNum = isNegative ? -parsedNum : parsedNum;
                        cleanedFields[col] = finalNum.ToString(CultureInfo.InvariantCulture);
                        continue;
                    }
                    cleanedFields[col] = withoutSymbol;
                    continue;
                }

                // Clean thousands separators if standard numeric format without letters
                if (toProcess.Contains(',') && !toProcess.Any(char.IsLetter))
                {
                    var withoutCommas = toProcess.Replace(",", "");
                    if (decimal.TryParse(withoutCommas, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                    {
                        var finalNum = isNegative ? -num : num;
                        cleanedFields[col] = finalNum.ToString(CultureInfo.InvariantCulture);
                        continue;
                    }
                }

                if (isNegative && decimal.TryParse(toProcess, NumberStyles.Float, CultureInfo.InvariantCulture, out var negNum))
                {
                    cleanedFields[col] = (-negNum).ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                // 5. Default cleaned string
                cleanedFields[col] = trimmed;
            }

            cleanedRows.Add(new TabularRow(row.RowId, cleanedFields));
        }

        var cleanedBatch = new TabularBatch(batch.SourceName, batch.Columns, cleanedRows);
        context.DataBatches["Batch"] = cleanedBatch;
        context.DataBatches["CleanedBatch"] = cleanedBatch;

        return Task.FromResult(new StageResult(
            StageName,
            IsSuccess: true,
            IsFatal: false,
            InputRows: inputRows,
            OutputRows: cleanedRows.Count,
            QuarantinedRows: 0,
            RulesFired: new[] { "TrimWhitespace", "CurrencyNormalization", "NullStandardization" },
            SummaryDetails: $"Standardized {cleanedRows.Count} Silver rows."
        ));
    }
}
