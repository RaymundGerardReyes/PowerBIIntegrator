using FluentAssertions;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.DataQuality;

public class CompositeKeyDedupeEngineTests
{
    [Fact]
    public void Deduplicate_MatchingCompositeKey_DeduplicatesCorrectly()
    {
        var engine = new CompositeKeyDedupeEngine();
        var columns = new[] { "CustomerId", "InvoiceId", "Amount" };
        var rows = new List<TabularRow>
        {
            new("row_1", new Dictionary<string, string?> { ["CustomerId"] = "C100", ["InvoiceId"] = "INV-01", ["Amount"] = "50" }),
            new("row_2", new Dictionary<string, string?> { ["CustomerId"] = "C100", ["InvoiceId"] = "INV-01", ["Amount"] = "55" }) // Collides on CustomerId + InvoiceId
        };

        var batch = new TabularBatch("Invoices", columns, rows);

        var (cleanBatch, clusters) = engine.Deduplicate(batch, new[] { "CustomerId", "InvoiceId" });

        cleanBatch.Rows.Should().HaveCount(1);
        clusters.Should().HaveCount(1);
        clusters[0].KeptRowId.Should().Be("row_1");
        clusters[0].DroppedRowIds.Should().Contain("row_2");
    }
}

