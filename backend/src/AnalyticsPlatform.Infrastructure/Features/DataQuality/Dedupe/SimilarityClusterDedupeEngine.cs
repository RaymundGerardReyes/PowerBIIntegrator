using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;

public class SimilarityClusterDedupeEngine
{
    public (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) Deduplicate(TabularBatch batch, string targetColumn, double threshold = 0.85)
    {
        var keptRows = new List<TabularRow>();
        var clusters = new List<DuplicateCluster>();
        int clusterCounter = 1;

        foreach (var row in batch.Rows)
        {
            var val = row.Fields.TryGetValue(targetColumn, out var v) ? v : null;
            if (string.IsNullOrWhiteSpace(val))
            {
                keptRows.Add(row);
                continue;
            }

            var existingMatch = keptRows.FirstOrDefault(k =>
            {
                var kVal = k.Fields.TryGetValue(targetColumn, out var kv) ? kv : null;
                if (string.IsNullOrWhiteSpace(kVal)) return false;
                return CalculateLevenshteinSimilarity(val, kVal) >= threshold;
            });

            if (existingMatch != null)
            {
                var simScore = CalculateLevenshteinSimilarity(val, existingMatch.Fields[targetColumn]!);
                clusters.Add(new DuplicateCluster(
                    $"sim_cluster_{clusterCounter++}",
                    "SimilarityClusterDedupe",
                    existingMatch.RowId,
                    new[] { row.RowId },
                    simScore,
                    $"Levenshtein similarity {simScore:P0} on column '{targetColumn}'"
                ));
            }
            else
            {
                keptRows.Add(row);
            }
        }

        return (new TabularBatch(batch.SourceName, batch.Columns, keptRows), clusters);
    }

    private static double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        if (s1.Equals(s2, StringComparison.OrdinalIgnoreCase)) return 1.0;
        int len1 = s1.Length, len2 = s2.Length;
        if (len1 == 0 || len2 == 0) return 0.0;

        int[,] d = new int[len1 + 1, len2 + 1];
        for (int i = 0; i <= len1; i++) d[i, 0] = i;
        for (int j = 0; j <= len2; j++) d[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        int distance = d[len1, len2];
        int maxLen = Math.Max(len1, len2);
        return 1.0 - ((double)distance / maxLen);
    }
}

