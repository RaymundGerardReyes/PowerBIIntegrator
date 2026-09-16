using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;

public class CompositeKeyDedupeEngine
{
    public (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) Deduplicate(TabularBatch batch, IReadOnlyList<string> keyColumns)
    {
        var seenKeys = new Dictionary<string, string>();
        var keptRows = new List<TabularRow>();
        var clusters = new List<DuplicateCluster>();
        int clusterCounter = 1;

        foreach (var row in batch.Rows)
        {
            var keyString = string.Join("::", keyColumns.Select(k => row.Fields.TryGetValue(k, out var v) ? v?.Trim() ?? "" : ""));
            if (string.IsNullOrWhiteSpace(keyString))
            {
                keptRows.Add(row);
                continue;
            }

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
