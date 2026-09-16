using FluentAssertions;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.DataQuality;

public class SimilarityClusterDedupeEngineTests
{
    [Fact]
    public void Deduplicate_NearDuplicateStrings_DetectsSimilarityCluster()
    {
        var engine = new SimilarityClusterDedupeEngine();
        var columns = new[] { "Name", "City" };
        var rows = new List<TabularRow>
        {
            new("row_1", new Dictionary<string, string?> { ["Name"] = "Microsoft Corp", ["City"] = "Redmond" }),
            new("row_2", new Dictionary<string, string?> { ["Name"] = "Microsoft Corp.", ["City"] = "Redmond" }) // Near duplicate with period
        };

        var batch = new TabularBatch("Companies", columns, rows);

        var (cleanBatch, clusters) = engine.Deduplicate(batch, "Name", threshold: 0.85);

        cleanBatch.Rows.Should().HaveCount(1);
        clusters.Should().HaveCount(1);
        clusters[0].KeptRowId.Should().Be("row_1");
        clusters[0].DroppedRowIds.Should().Contain("row_2");
        clusters[0].ConfidenceScore.Should().BeGreaterThan(0.85);
    }
}
