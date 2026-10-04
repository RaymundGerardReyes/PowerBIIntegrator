using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
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

    [Fact]
    public void Deduplicate_MultipleRowsWithAllNullOrEmptyCompositeKeys_DoesNotTreatAsDuplicates()
    {
        var engine = new CompositeKeyDedupeEngine();
        var columns = new[] { "CustomerId", "InvoiceId", "Amount" };
        var rows = new List<TabularRow>
        {
            new("row_1", new Dictionary<string, string?> { ["CustomerId"] = null, ["InvoiceId"] = null, ["Amount"] = "10" }),
            new("row_2", new Dictionary<string, string?> { ["CustomerId"] = " ", ["InvoiceId"] = "", ["Amount"] = "20" }),
            new("row_3", new Dictionary<string, string?> { ["CustomerId"] = "C1", ["InvoiceId"] = "INV-1", ["Amount"] = "30" })
        };

        var batch = new TabularBatch("Invoices", columns, rows);

        var (cleanBatch, clusters) = engine.Deduplicate(batch, new[] { "CustomerId", "InvoiceId" });

        // Rows with empty/null keys are preserved and not clustered together as false collisions on "::"
        cleanBatch.Rows.Should().HaveCount(3);
        clusters.Should().BeEmpty();
    }
}

