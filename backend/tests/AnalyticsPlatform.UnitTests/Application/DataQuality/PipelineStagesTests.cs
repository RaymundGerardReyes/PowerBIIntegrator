using FluentAssertions;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.DataQuality;

public class PipelineStagesTests
{
    [Fact]
    public async Task ProfilingStage_CalculatesMetricsCorrectly()
    {
        var stage = new ProfilingStage();
        var columns = new[] { "Id", "Department", "Cost" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["Id"] = "1", ["Department"] = "IT", ["Cost"] = "500" }),
            new("row_1", new Dictionary<string, string?> { ["Id"] = "2", ["Department"] = "HR", ["Cost"] = "750" }),
            new("row_2", new Dictionary<string, string?> { ["Id"] = "3", ["Department"] = "IT", ["Cost"] = "" }) // Null/empty cost
        };
        var batch = new TabularBatch("Employees", columns, rows);

        var context = new PipelineContext("run-p1", "employees.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["DatasetName"] = "Employees"
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InputRows.Should().Be(3);
        result.OutputRows.Should().Be(3);
        result.RulesFired.Should().Contain("ProfileEngine");

        context.DataBatches.Should().ContainKey("Profile");
        var profile = context.DataBatches["Profile"] as DatasetProfile;
        profile.Should().NotBeNull();
        profile!.ColumnProfiles.Should().HaveCount(3);

        var costProfile = profile.ColumnProfiles.First(c => c.ColumnName == "Cost");
        costProfile.NullCount.Should().Be(1);
        costProfile.DistinctCount.Should().Be(2);

        var deptProfile = profile.ColumnProfiles.First(c => c.ColumnName == "Department");
        deptProfile.NullCount.Should().Be(0);
        deptProfile.DistinctCount.Should().Be(2);
    }

    [Fact]
    public async Task DeduplicationStage_WithHashDedupeEngine_QuarantinesExactDuplicates()
    {
        var hashEngine = new HashDedupeEngine();
        var stage = new DeduplicationStage(hashEngine, null);

        var columns = new[] { "Name", "City" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["Name"] = "Alice", ["City"] = "London" }),
            new("row_1", new Dictionary<string, string?> { ["Name"] = "Bob", ["City"] = "Paris" }),
            new("row_2", new Dictionary<string, string?> { ["Name"] = "Alice", ["City"] = "London" }) // Duplicate
        };
        var batch = new TabularBatch("Users", columns, rows);

        var context = new PipelineContext("run-d1", "users.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InputRows.Should().Be(3);
        result.OutputRows.Should().Be(2);
        result.QuarantinedRows.Should().Be(1);
        result.RulesFired.Should().Contain("ExactHashDedupe");

        context.DataBatches.Should().ContainKey("CleanBatch");
        var cleanBatch = context.DataBatches["CleanBatch"] as TabularBatch;
        cleanBatch!.Rows.Should().HaveCount(2);

        var clusters = context.DataBatches["DuplicateClusters"] as IReadOnlyList<DuplicateCluster>;
        clusters.Should().NotBeNull();
        clusters.Should().HaveCount(1);
        clusters![0].KeptRowId.Should().Be("row_0");
        clusters[0].DroppedRowIds.Should().Contain("row_2");
    }

    [Fact]
    public async Task DeduplicationStage_WithCompositeKeyEngine_IdentifiesKeyCollisions()
    {
        var compositeEngine = new CompositeKeyDedupeEngine();
        var stage = new DeduplicationStage(null, compositeEngine);

        var columns = new[] { "CustomerId", "InvoiceId", "Total" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["CustomerId"] = "C1", ["InvoiceId"] = "INV-01", ["Total"] = "100" }),
            new("row_1", new Dictionary<string, string?> { ["CustomerId"] = "C1", ["InvoiceId"] = "INV-01", ["Total"] = "105" }), // Collision on CustomerId + InvoiceId
            new("row_2", new Dictionary<string, string?> { ["CustomerId"] = "C2", ["InvoiceId"] = "INV-02", ["Total"] = "200" })
        };
        var batch = new TabularBatch("Invoices", columns, rows);

        var context = new PipelineContext("run-d2", "invoices.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["KeyColumns"] = new[] { "CustomerId", "InvoiceId" }
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InputRows.Should().Be(3);
        result.OutputRows.Should().Be(2);
        result.QuarantinedRows.Should().Be(1);
        result.RulesFired.Should().Contain("CompositeKeyDedupe");

        var clusters = context.DataBatches["DuplicateClusters"] as IReadOnlyList<DuplicateCluster>;
        clusters!.Should().HaveCount(1);
        clusters![0].KeptRowId.Should().Be("row_0");
        clusters[0].DroppedRowIds.Should().Contain("row_1");
    }

    [Fact]
    public async Task CleaningStage_TrimsWhitespaceAndStripsCurrencySymbols()
    {
        var stage = new CleaningStage();

        var columns = new[] { "Department", "Cost", "Status" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?>
            {
                ["Department"] = "  IT  ",
                ["Cost"] = "₱1,500",
                ["Status"] = "ACTIVE"
            }),
            new("row_1", new Dictionary<string, string?>
            {
                ["Department"] = "HR",
                ["Cost"] = "$2,000.50",
                ["Status"] = "N/A"
            }),
            new("row_2", new Dictionary<string, string?>
            {
                ["Department"] = "Finance",
                ["Cost"] = "null",
                ["Status"] = " - "
            })
        };
        var batch = new TabularBatch("RawData", columns, rows);

        var context = new PipelineContext("run-c1", "raw.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InputRows.Should().Be(3);
        result.OutputRows.Should().Be(3);
        result.RulesFired.Should().Contain("CurrencyNormalization");
        result.RulesFired.Should().Contain("TrimWhitespace");
        result.RulesFired.Should().Contain("NullStandardization");

        var cleanedBatch = context.DataBatches["CleanedBatch"] as TabularBatch;
        cleanedBatch.Should().NotBeNull();

        // Row 0 assertions
        cleanedBatch!.Rows[0].Fields["Department"].Should().Be("IT");
        cleanedBatch.Rows[0].Fields["Cost"].Should().Be("1500");
        cleanedBatch.Rows[0].Fields["Status"].Should().Be("ACTIVE");

        // Row 1 assertions (currency parsed, N/A converted to null)
        cleanedBatch.Rows[1].Fields["Cost"].Should().Be("2000.50");
        cleanedBatch.Rows[1].Fields["Status"].Should().BeNull();

        // Row 2 assertions ("null" and "-" converted to null)
        cleanedBatch.Rows[2].Fields["Cost"].Should().BeNull();
        cleanedBatch.Rows[2].Fields["Status"].Should().BeNull();
    }

    [Fact]
    public async Task TransformationStage_BuildsGoldSchemaAndModel()
    {
        var stage = new TransformationStage();

        var profile = new DatasetProfile("GoldSales", "sales.csv", 100);
        profile.AddColumnProfile(new ColumnProfile("Product", "String", 100, 0, 0, 10, "A", "Z", new[] { "A" }, "^.*$", "Low"));
        profile.AddColumnProfile(new ColumnProfile("Revenue", "Decimal", 100, 0, 0, 50, "10", "1000", new[] { "100" }, @"^\d+(\.\d+)?$", "Medium"));

        var columns = new[] { "Product", "Revenue" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["Product"] = "Widget", ["Revenue"] = "100" })
        };
        var batch = new TabularBatch("Sales", columns, rows);

        var context = new PipelineContext("run-t1", "sales.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["Profile"] = profile,
            ["TargetGoldTable"] = "GoldSales"
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.RulesFired.Should().Contain("GoldAggregationRule");
        result.SummaryDetails.Should().Contain("GoldSales");

        context.DataBatches.Should().ContainKey("GoldModel");
        var model = context.DataBatches["GoldModel"] as AnalyticsPlatform.Domain.Features.Analytics.Entities.AnalyticsModel;
        model.Should().NotBeNull();
        model!.Tables.Should().ContainSingle(t => t.Name == "GoldSales");
        model.Measures.Should().Contain(m => m.Name == "Total_Revenue");
    }

    [Fact]
    public async Task FullPipeline_EndToEndChaining_ProducesCleanGoldModel()
    {
        var hashEngine = new HashDedupeEngine();
        var compositeEngine = new CompositeKeyDedupeEngine();

        var stages = new IDataQualityStage[]
        {
            new ProfilingStage(),
            new DeduplicationStage(hashEngine, compositeEngine),
            new CleaningStage(),
            new TransformationStage()
        };

        var orchestrator = new PipelineOrchestrator(stages);

        var columns = new[] { "EmpId", "Name", "Cost" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["EmpId"] = "001", ["Name"] = "  John  ", ["Cost"] = "₱500" }),
            new("row_1", new Dictionary<string, string?> { ["EmpId"] = "002", ["Name"] = "Mary", ["Cost"] = "₱750" }),
            new("row_2", new Dictionary<string, string?> { ["EmpId"] = "001", ["Name"] = "  John  ", ["Cost"] = "₱500" }) // Duplicate row
        };
        var batch = new TabularBatch("Employees", columns, rows);

        var context = new PipelineContext("run-e2e", "employees.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["DatasetName"] = "Employees",
            ["TargetGoldTable"] = "CuratedEmployees"
        });

        var result = await orchestrator.RunAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StageSummaries.Should().HaveCount(4);
        result.StageSummaries.Select(s => s.StageName).Should().ContainInOrder("Profiling", "Deduplication", "Cleaning", "Transformation");

        // Profiling stage check
        result.StageSummaries[0].InputRowCount.Should().Be(3);

        // Deduplication stage check (3 rows in -> 2 rows out, 1 quarantined)
        result.StageSummaries[1].InputRowCount.Should().Be(3);
        result.StageSummaries[1].OutputRowCount.Should().Be(2);
        result.StageSummaries[1].QuarantinedRowCount.Should().Be(1);

        // Cleaning stage check
        result.StageSummaries[2].InputRowCount.Should().Be(2);
        result.StageSummaries[2].OutputRowCount.Should().Be(2);

        // Transformation stage check
        result.StageSummaries[3].OutputRowCount.Should().Be(2);
        context.DataBatches.Should().ContainKey("GoldModel");

        var goldModel = context.DataBatches["GoldModel"] as AnalyticsPlatform.Domain.Features.Analytics.Entities.AnalyticsModel;
        goldModel.Should().NotBeNull();
        goldModel!.Tables.Should().ContainSingle(t => t.Name == "CuratedEmployees");
        goldModel.Measures.Should().Contain(m => m.Name == "Total_Cost");
    }

    [Fact]
    public async Task CleaningStage_CleansAccountingParenthesesAndSuffixCurrencies()
    {
        var stage = new CleaningStage();

        var columns = new[] { "Amount", "Balance" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["Amount"] = "(₱500)", ["Balance"] = "1,500 €" }),
            new("row_1", new Dictionary<string, string?> { ["Amount"] = "($1,200.50)", ["Balance"] = "2000 ₱" })
        };
        var batch = new TabularBatch("Financials", columns, rows);

        var context = new PipelineContext("run-f1", "fin.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cleaned = context.DataBatches["CleanedBatch"] as TabularBatch;
        cleaned.Should().NotBeNull();
        cleaned!.Rows[0].Fields["Amount"].Should().Be("-500");
        cleaned.Rows[0].Fields["Balance"].Should().Be("1500");
        cleaned.Rows[1].Fields["Amount"].Should().Be("-1200.50");
        cleaned.Rows[1].Fields["Balance"].Should().Be("2000");
    }

    [Fact]
    public async Task CleaningStage_AppliesValueMappingsWhenProvided()
    {
        var stage = new CleaningStage();

        var columns = new[] { "Status" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["Status"] = "ACTIVE" }),
            new("row_1", new Dictionary<string, string?> { ["Status"] = "act" })
        };
        var batch = new TabularBatch("StatusData", columns, rows);

        var mappings = new Dictionary<string, IDictionary<string, string>>
        {
            ["Status"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ACTIVE"] = "Active",
                ["act"] = "Active"
            }
        };

        var context = new PipelineContext("run-m1", "status.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["ValueMappings"] = mappings
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cleaned = context.DataBatches["CleanedBatch"] as TabularBatch;
        cleaned!.Rows[0].Fields["Status"].Should().Be("Active");
        cleaned.Rows[1].Fields["Status"].Should().Be("Active");
    }
}
