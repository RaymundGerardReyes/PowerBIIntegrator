using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Repositories;

public class InMemoryAdvisoryRunRepository : IAdvisoryRunRepository
{
    private readonly Dictionary<string, PipelineRunResult> _runs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DatasetProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<DuplicateCluster>> _clusters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SchemaViolationSummary>> _violations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TransformationPlan> _plans = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ChartSuggestionSummary>> _chartSuggestions = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAdvisoryRunRepository()
    {
        SeedRealisticData();
    }

    private void SeedRealisticData()
    {
        const string demoRunId = "run-demo-001";
        const string demoDatasetId = "ds-sales-001";
        const string demoPlanId = "plan-merge-001";

        // Seed PipelineRunResult
        var run = new PipelineRunResult(
            runId: demoRunId,
            sourceReference: "SalesTransactions.csv",
            startedAtUtc: DateTime.UtcNow.AddMinutes(-5),
            completedAtUtc: DateTime.UtcNow,
            isSuccess: true);

        run.AddStageSummary(new StageRunSummary(
            "BronzeIngestion",
            true,
            1042,
            1042,
            0,
            new[] { "CsvIngestionRule" },
            "Ingested 1,042 raw Bronze rows."));

        run.AddStageSummary(new StageRunSummary(
            "SilverDeduplication",
            true,
            1042,
            1000,
            42,
            new[] { "ExactHashRule-v2", "CompositeKeyRule-CustInv" },
            "42 duplicate rows identified and quarantined."));

        run.AddStageSummary(new StageRunSummary(
            "GoldTransformation",
            true,
            1000,
            1000,
            0,
            new[] { "TransformationSequencingRule" },
            "Clean tabular dataset published to Gold model."));

        _runs[demoRunId] = run;

        // Seed DatasetProfile
        var profile = new DatasetProfile("SalesTransactions", "SalesTransactions.csv", 1000);
        profile.AddColumnProfile(new ColumnProfile("TransactionId", "string", 1000, 0, 0.0, 1000, "TX001", "TX999", new[] { "TX001" }, "^TX[0-9]+$", "Unique"));
        profile.AddColumnProfile(new ColumnProfile("CustomerId", "string", 1000, 5, 0.005, 240, "CUST001", "CUST999", new[] { "CUST001" }, "^CUST[0-9]+$", "HighCardinality"));
        profile.AddColumnProfile(new ColumnProfile("UnitCost", "decimal", 1000, 12, 0.012, 85, "1.50", "999.00", new[] { "19.99" }, "^[0-9]+(\\.[0-9]{2})?$", "MediumCardinality"));
        profile.AddColumnProfile(new ColumnProfile("OrderDate", "datetime", 1000, 0, 0.0, 365, "2026-01-01", "2026-12-31", new[] { "2026-06-15" }, "^\\d{4}-\\d{2}-\\d{2}$", "MediumCardinality"));

        _profiles[demoDatasetId] = profile;
        _profiles[demoRunId] = profile;

        // Seed Duplicate Clusters
        _clusters[demoRunId] = new List<DuplicateCluster>
        {
            new DuplicateCluster(
                clusterId: "cluster-01",
                ruleFired: "ExactHashRule-v2",
                keptRowId: "102",
                droppedRowIds: new[] { "103", "104", "105" },
                confidenceScore: 1.0,
                reasonCode: "Exact SHA-256 row content fingerprinting matched identical customer and invoice tuples."),
            new DuplicateCluster(
                clusterId: "cluster-02",
                ruleFired: "CompositeKeyRule-CustInv",
                keptRowId: "210",
                droppedRowIds: new[] { "211" },
                confidenceScore: 0.95,
                reasonCode: "Composite key (CustomerId + InvoiceNumber) conflict detected across multiple dates.")
        };

        // Seed Schema Violations
        _violations[demoRunId] = new List<SchemaViolationSummary>
        {
            new(
                RuleId: "SchemaCompatibilityRule-TypeMismatch",
                RuleName: "ColumnTypeValidationRule",
                ColumnName: "UnitCost",
                ExpectedType: "Decimal",
                ActualType: "String",
                Severity: "Error")
        };

        // Seed Transformation Plan
        var plan = new TransformationPlan("Standardize-Sales-Dataset", "GoldSales", version: 1);
        plan.AddStep(new TransformationStep(1, "FilterQuarantine", "Status", "SchemaCompatibilityRule", new Dictionary<string, string>()));
        plan.AddStep(new TransformationStep(2, "Deduplicate", "CustomerId", "ExactHashRule-v2", new Dictionary<string, string>()));
        plan.AddStep(new TransformationStep(3, "TypeCast", "UnitCost", "decimal", new Dictionary<string, string>()));

        _plans[demoPlanId] = plan;
        _plans[demoRunId] = plan;

        // Seed Chart Suggestions
        _chartSuggestions[demoRunId] = new List<ChartSuggestionSummary>
        {
            new(
                RuleId: "VisualMappingRule-BarChart-SalesByRegion",
                ChartType: "ClusteredBarChart",
                SuggestedMeasure: "Sum(UnitCost)",
                CategoryColumn: "Region",
                Reason: "High cardinality categorical column with continuous monetary measure matches clustered bar distribution.")
        };
    }

    public Task<PipelineRunResult?> GetRunResultAsync(string runId, CancellationToken ct = default)
    {
        if (_runs.TryGetValue(runId, out var result))
        {
            return Task.FromResult<PipelineRunResult?>(result);
        }
        if (_runs.TryGetValue("run-demo-001", out var demo))
        {
            return Task.FromResult<PipelineRunResult?>(demo);
        }
        return Task.FromResult<PipelineRunResult?>(null);
    }

    public Task<DatasetProfile?> GetDatasetProfileAsync(string datasetId, CancellationToken ct = default)
    {
        if (_profiles.TryGetValue(datasetId, out var profile))
        {
            return Task.FromResult<DatasetProfile?>(profile);
        }
        if (_profiles.TryGetValue("ds-sales-001", out var demo))
        {
            return Task.FromResult<DatasetProfile?>(demo);
        }
        return Task.FromResult<DatasetProfile?>(null);
    }

    public Task<IReadOnlyList<DuplicateCluster>> GetDuplicateClustersAsync(string runId, CancellationToken ct = default)
    {
        if (_clusters.TryGetValue(runId, out var clusters))
        {
            return Task.FromResult<IReadOnlyList<DuplicateCluster>>(clusters);
        }
        if (_clusters.TryGetValue("run-demo-001", out var demo))
        {
            return Task.FromResult<IReadOnlyList<DuplicateCluster>>(demo);
        }
        return Task.FromResult<IReadOnlyList<DuplicateCluster>>(Array.Empty<DuplicateCluster>());
    }

    public Task<IReadOnlyList<SchemaViolationSummary>> GetSchemaViolationsAsync(string runId, CancellationToken ct = default)
    {
        if (_violations.TryGetValue(runId, out var violations))
        {
            return Task.FromResult<IReadOnlyList<SchemaViolationSummary>>(violations);
        }
        if (_violations.TryGetValue("run-demo-001", out var demo))
        {
            return Task.FromResult<IReadOnlyList<SchemaViolationSummary>>(demo);
        }
        return Task.FromResult<IReadOnlyList<SchemaViolationSummary>>(Array.Empty<SchemaViolationSummary>());
    }

    public Task<TransformationPlan?> GetTransformationPlanAsync(string planId, CancellationToken ct = default)
    {
        if (_plans.TryGetValue(planId, out var plan))
        {
            return Task.FromResult<TransformationPlan?>(plan);
        }
        if (_plans.TryGetValue("plan-merge-001", out var demo))
        {
            return Task.FromResult<TransformationPlan?>(demo);
        }
        return Task.FromResult<TransformationPlan?>(null);
    }

    public Task<IReadOnlyList<ChartSuggestionSummary>> GetChartSuggestionsAsync(string runId, CancellationToken ct = default)
    {
        if (_chartSuggestions.TryGetValue(runId, out var suggestions))
        {
            return Task.FromResult<IReadOnlyList<ChartSuggestionSummary>>(suggestions);
        }
        if (_chartSuggestions.TryGetValue("run-demo-001", out var demo))
        {
            return Task.FromResult<IReadOnlyList<ChartSuggestionSummary>>(demo);
        }
        return Task.FromResult<IReadOnlyList<ChartSuggestionSummary>>(Array.Empty<ChartSuggestionSummary>());
    }
}

