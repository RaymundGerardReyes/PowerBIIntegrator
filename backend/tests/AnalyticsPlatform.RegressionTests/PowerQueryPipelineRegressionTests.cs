using FluentAssertions;
using AnalyticsPlatform.Application.Features.Analytics.Services;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Infrastructure.Features.DataQuality.Dedupe;
using Xunit;
using ColumnDataType = AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType;

namespace AnalyticsPlatform.RegressionTests;

public class PowerQueryPipelineRegressionTests
{
    [Fact]
    public void SanitizeIdentifier_EnforcesCleanPowerQueryAndTmdlNames()
    {
        // Must strip extensions, leading numbers, invalid characters, and prepend T_ if starting with digit
        AnalyticsModelFactory.SanitizeIdentifier("6 titanic.csv").Should().Be("titanic");
        AnalyticsModelFactory.SanitizeIdentifier("123 Sales - North & South.xlsx").Should().Be("Sales_North_South");
        AnalyticsModelFactory.SanitizeIdentifier("2026Q1_Report").Should().Be("T_2026Q1_Report");
        AnalyticsModelFactory.SanitizeIdentifier("emp_id#_data.pbip").Should().Be("emp_id_data");
        AnalyticsModelFactory.SanitizeIdentifier("Customer Orders (2026)").Should().Be("Customer_Orders_2026");
    }

    [Fact]
    public void PowerQueryMGeneration_GeneratesSequentialAppliedStepsNarrative()
    {
        var tempCsv = Path.Combine(Path.GetTempPath(), $"pq_test_{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(tempCsv, "Department,Revenue,Units\nIT,1500.50,10\n");

            var schema = new List<ColumnSchema>
            {
                new(0, "Department", ColumnDataType.String, false, new[] { "IT" }),
                new(1, "Revenue", ColumnDataType.Decimal, false, new[] { "1500.50" }),
                new(2, "Units", ColumnDataType.Integer, false, new[] { "10" })
            };

            var model = AnalyticsModelFactory.CreateFromDataSource(
                "SalesSummary",
                schema,
                connectionOrPath: tempCsv,
                sourceType: DataSourceType.Csv);

            var table = model.Tables.First(t => t.Name == "SalesSummary");
            table.MQueryPartition.Should().NotBeNullOrWhiteSpace();

            var mQuery = table.MQueryPartition!;
            mQuery.Should().Contain("Source = Csv.Document(File.Contents(");
            mQuery.Should().Contain("#\"Promoted Headers\" = Table.PromoteHeaders(Source, [PromoteAllScalars=true])");
            mQuery.Should().Contain("#\"Changed Type\" = Table.TransformColumnTypes(#\"Promoted Headers\", {");
            mQuery.Should().Contain("{\"Revenue\", type number}");
            mQuery.Should().Contain("{\"Units\", Int64.Type}");
            mQuery.Should().Contain("#\"Cleaned Text\" = Table.TransformColumns(#\"Changed Type\", {{\"Department\", Text.Trim, type text}})");
            mQuery.Should().Contain("in\n    #\"Cleaned Text\"");

            // Invariant: DAX measure parity
            model.Measures.Should().Contain(m => m.Name == "TotalRows");
            model.Measures.Should().Contain(m => m.Name == "Total_Revenue");
            model.Measures.Should().Contain(m => m.Name == "Average_Revenue");
            model.Measures.Should().Contain(m => m.Name == "Total_Units");
        }
        finally
        {
            if (File.Exists(tempCsv))
            {
                try { File.Delete(tempCsv); } catch { }
            }
        }
    }

    [Fact]
    public async Task CleaningStage_Layer1TechnicalCleaning_NormalizesVariedCurrenciesAndParentheses()
    {
        var stage = new CleaningStage();

        var columns = new[] { "Item", "Cost", "Balance", "Fee", "Profit" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?>
            {
                ["Item"] = "  Server A \t\n ",
                ["Cost"] = "₱1,500.75",
                ["Balance"] = "($450.00)",     // Accounting negative
                ["Fee"] = "25.50 €",           // Suffix currency
                ["Profit"] = "¥10000"          // Yen symbol
            }),
            new("row_1", new Dictionary<string, string?>
            {
                ["Item"] = "License B",
                ["Cost"] = "£900",
                ["Balance"] = "(₱1,200)",      // Accounting negative with peso
                ["Fee"] = "null",              // Textual null
                ["Profit"] = " - "             // Dash blank
            })
        };
        var batch = new TabularBatch("FinancialRecords", columns, rows);

        var context = new PipelineContext("run-reg1", "fin.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cleaned = context.DataBatches["CleanedBatch"] as TabularBatch;
        cleaned.Should().NotBeNull();

        // Row 0 assertions
        cleaned!.Rows[0].Fields["Item"].Should().Be("Server A");
        cleaned.Rows[0].Fields["Cost"].Should().Be("1500.75");
        cleaned.Rows[0].Fields["Balance"].Should().Be("-450.00");
        cleaned.Rows[0].Fields["Fee"].Should().Be("25.50");
        cleaned.Rows[0].Fields["Profit"].Should().Be("10000");

        // Row 1 assertions
        cleaned.Rows[1].Fields["Cost"].Should().Be("900");
        cleaned.Rows[1].Fields["Balance"].Should().Be("-1200");
        cleaned.Rows[1].Fields["Fee"].Should().BeNull();
        cleaned.Rows[1].Fields["Profit"].Should().BeNull();
    }

    [Fact]
    public async Task DeduplicationStage_Layer3BusinessValidation_GrainAndCompositeKeyIntegrity()
    {
        var hashEngine = new HashDedupeEngine();
        var compositeEngine = new CompositeKeyDedupeEngine();
        var stage = new DeduplicationStage(hashEngine, compositeEngine);

        // Same EmployeeId across different systems are distinct business records (not duplicates!)
        var columns = new[] { "EmployeeId", "System", "AccessLevel" };
        var rows = new List<TabularRow>
        {
            new("row_0", new Dictionary<string, string?> { ["EmployeeId"] = "001", ["System"] = "Zoho", ["AccessLevel"] = "Admin" }),
            new("row_1", new Dictionary<string, string?> { ["EmployeeId"] = "001", ["System"] = "Gmail", ["AccessLevel"] = "User" }),
            new("row_2", new Dictionary<string, string?> { ["EmployeeId"] = "001", ["System"] = "ERP", ["AccessLevel"] = "User" }),
            new("row_3", new Dictionary<string, string?> { ["EmployeeId"] = "001", ["System"] = "Zoho", ["AccessLevel"] = "Admin" }) // Duplicate access transaction
        };
        var batch = new TabularBatch("AccessLogs", columns, rows);

        var context = new PipelineContext("run-grain", "access.csv", new Dictionary<string, object>
        {
            ["Batch"] = batch,
            ["KeyColumns"] = new[] { "EmployeeId", "System" }
        });

        var result = await stage.ExecuteAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InputRows.Should().Be(4);
        result.OutputRows.Should().Be(3);
        result.QuarantinedRows.Should().Be(1);

        var clusters = context.DataBatches["DuplicateClusters"] as IReadOnlyList<DuplicateCluster>;
        clusters.Should().HaveCount(1);
        clusters![0].KeptRowId.Should().Be("row_0");
        clusters[0].DroppedRowIds.Should().Contain("row_3");

        var cleanBatch = context.DataBatches["CleanBatch"] as TabularBatch;
        cleanBatch!.Rows.Should().HaveCount(3);
        cleanBatch.Rows.Select(r => r.Fields["System"]).Should().BeEquivalentTo(new[] { "Zoho", "Gmail", "ERP" });
    }

    [Fact]
    public async Task PipelineOrchestrator_ShortCircuitsOnFatalStageFailure()
    {
        var fatalStage = new MockFatalStage("ValidationStage");
        var subsequentStage = new MockTrackingStage("SubsequentStage");

        var orchestrator = new PipelineOrchestrator(new IDataQualityStage[] { fatalStage, subsequentStage });
        var context = new PipelineContext("run-fatal", "source.csv", new Dictionary<string, object>());

        var result = await orchestrator.RunAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StageSummaries.Should().HaveCount(1);
        result.StageSummaries[0].StageName.Should().Be("ValidationStage");
        subsequentStage.WasExecuted.Should().BeFalse();
    }

    private sealed class MockFatalStage : IDataQualityStage
    {
        public string StageName { get; }

        public MockFatalStage(string name) => StageName = name;

        public Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
        {
            return Task.FromResult(new StageResult(
                StageName,
                IsSuccess: false,
                IsFatal: true,
                InputRows: 10,
                OutputRows: 0,
                QuarantinedRows: 10,
                RulesFired: new[] { "FatalRule" },
                SummaryDetails: "Fatal schema violation halted pipeline."
            ));
        }
    }

    private sealed class MockTrackingStage : IDataQualityStage
    {
        public string StageName { get; }
        public bool WasExecuted { get; private set; }

        public MockTrackingStage(string name) => StageName = name;

        public Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
        {
            WasExecuted = true;
            return Task.FromResult(new StageResult(
                StageName,
                IsSuccess: true,
                IsFatal: false,
                InputRows: 0,
                OutputRows: 0,
                QuarantinedRows: 0,
                RulesFired: Array.Empty<string>(),
                SummaryDetails: "Success"
            ));
        }
    }
}
