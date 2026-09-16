using System.Security.Cryptography;
using System.Text;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;

public class HashDedupeEngine
{
    public (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) Deduplicate(TabularBatch batch)
    {
        var seenHashes = new Dictionary<string, string>(); // Hash -> KeptRowId
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
}

