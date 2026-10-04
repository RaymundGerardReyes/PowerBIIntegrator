using System.Security.Cryptography;
using System.Text;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;

public class DeduplicationStage : IDataQualityStage
{
    private readonly IHashDedupeEngine? _hashDedupeEngine;
    private readonly ICompositeKeyDedupeEngine? _compositeKeyDedupeEngine;

    public string StageName => "Deduplication";

    public DeduplicationStage(
        IHashDedupeEngine? hashDedupeEngine = null,
        ICompositeKeyDedupeEngine? compositeKeyDedupeEngine = null)
    {
        _hashDedupeEngine = hashDedupeEngine;
        _compositeKeyDedupeEngine = compositeKeyDedupeEngine;
    }

    public Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
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

        var inputRows = batch?.Rows.Count ?? 0;
        if (batch == null || inputRows == 0)
        {
            var emptyClusters = Array.Empty<DuplicateCluster>();
            context.DataBatches["DuplicateClusters"] = emptyClusters;
            return Task.FromResult(new StageResult(
                StageName,
                IsSuccess: true,
                IsFatal: false,
                InputRows: 0,
                OutputRows: 0,
                QuarantinedRows: 0,
                RulesFired: new[] { "ExactHashDedupe" },
                SummaryDetails: "0 duplicate rows detected; entire dataset contains unique records."
            ));
        }

        TabularBatch cleanBatch;
        IReadOnlyList<DuplicateCluster> clusters;
        string ruleName;

        if (context.DataBatches.TryGetValue("KeyColumns", out var kcObj) && kcObj is IReadOnlyList<string> keyCols && keyCols.Count > 0)
        {
            ruleName = "CompositeKeyDedupe";
            if (_compositeKeyDedupeEngine != null)
            {
                (cleanBatch, clusters) = _compositeKeyDedupeEngine.Deduplicate(batch, keyCols);
            }
            else
            {
                (cleanBatch, clusters) = FallbackCompositeKeyDedupe(batch, keyCols);
            }
        }
        else
        {
            ruleName = "ExactHashDedupe";
            if (_hashDedupeEngine != null)
            {
                (cleanBatch, clusters) = _hashDedupeEngine.Deduplicate(batch);
            }
            else
            {
                (cleanBatch, clusters) = FallbackHashDedupe(batch);
            }
        }

        var outputRows = cleanBatch.Rows.Count;
        var quarantinedRows = inputRows - outputRows;

        context.DataBatches["Batch"] = cleanBatch;
        context.DataBatches["CleanBatch"] = cleanBatch;
        context.DataBatches["DuplicateClusters"] = clusters;

        var details = quarantinedRows > 0
            ? $"{quarantinedRows} duplicate rows quarantined."
            : "0 duplicate rows detected; entire dataset contains unique records.";

        return Task.FromResult(new StageResult(
            StageName,
            IsSuccess: true,
            IsFatal: false,
            InputRows: inputRows,
            OutputRows: outputRows,
            QuarantinedRows: quarantinedRows,
            RulesFired: new[] { ruleName },
            SummaryDetails: details
        ));
    }

    private static (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) FallbackHashDedupe(TabularBatch batch)
    {
        var seenHashes = new Dictionary<string, string>();
        var keptRows = new List<TabularRow>();
        var clusters = new List<DuplicateCluster>();
        int clusterCounter = 1;

        foreach (var row in batch.Rows)
        {
            var canonicalRepresentation = string.Join("|", batch.Columns.OrderBy(c => c).Select(c => row.Fields.TryGetValue(c, out var val) ? val?.Trim().ToLowerInvariant() ?? "" : ""));
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRepresentation));
            var hashString = Convert.ToHexString(hashBytes);

            if (seenHashes.TryGetValue(hashString, out var keptRowId))
            {
                clusters.Add(new DuplicateCluster(
                    $"cluster_{clusterCounter++}",
                    "ExactHashDedupe",
                    keptRowId,
                    new[] { row.RowId },
                    1.0,
                    "Exact SHA-256 row payload match"
                ));
            }
            else
            {
                seenHashes[hashString] = row.RowId;
                keptRows.Add(row);
            }
        }

        return (new TabularBatch(batch.SourceName, batch.Columns, keptRows), clusters);
    }

    private static (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) FallbackCompositeKeyDedupe(TabularBatch batch, IReadOnlyList<string> keyColumns)
    {
        var seenKeys = new Dictionary<string, string>();
        var keptRows = new List<TabularRow>();
        var clusters = new List<DuplicateCluster>();
        int clusterCounter = 1;

        foreach (var row in batch.Rows)
        {
            bool allKeysEmpty = keyColumns.All(k => !row.Fields.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v));
            if (allKeysEmpty)
            {
                keptRows.Add(row);
                continue;
            }

            var keyString = string.Join("::", keyColumns.Select(k => row.Fields.TryGetValue(k, out var v) ? v?.Trim() ?? "" : ""));

            if (seenKeys.TryGetValue(keyString, out var keptRowId))
            {
                clusters.Add(new DuplicateCluster(
                    $"ck_cluster_{clusterCounter++}",
                    "CompositeKeyDedupe",
                    keptRowId,
                    new[] { row.RowId },
                    1.0,
                    $"Composite business key collision on [{string.Join(", ", keyColumns)}]"
                ));
            }
            else
            {
                seenKeys[keyString] = row.RowId;
                keptRows.Add(row);
            }
        }

        return (new TabularBatch(batch.SourceName, batch.Columns, keptRows), clusters);
    }
}
