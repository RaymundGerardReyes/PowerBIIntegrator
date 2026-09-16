using FluentAssertions;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.DataQuality;

public class HashDedupeEngineTests
{
    [Fact]
    public void Deduplicate_IdenticalRows_RemovesDuplicateAndCreatesCluster()
    {
        var engine = new HashDedupeEngine();
        var columns = new[] { "Name", "City" };
        var rows = new List<TabularRow>
        {
            new("row_1", new Dictionary<string, string?> { ["Name"] = "Alice", ["City"] = "New York" }),
            new("row_2", new Dictionary<string, string?> { ["Name"] = "Bob", ["City"] = "London" }),
            new("row_3", new Dictionary<string, string?> { ["Name"] = "alice ", ["City"] = "new york" }) // Exact hash duplicate after normalization
        };

        var batch = new TabularBatch("People", columns, rows);

        var (cleanBatch, clusters) = engine.Deduplicate(batch);

        cleanBatch.Rows.Should().HaveCount(2);
        clusters.Should().HaveCount(1);
        clusters[0].KeptRowId.Should().Be("row_1");
        clusters[0].DroppedRowIds.Should().Contain("row_3");
    }
}

