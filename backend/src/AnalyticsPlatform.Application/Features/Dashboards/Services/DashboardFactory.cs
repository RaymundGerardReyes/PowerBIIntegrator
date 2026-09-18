using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Application.Features.Dashboards.Services;

public static class DashboardFactory
{
    private static readonly HashSet<string> IdentifierColumnNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "full_name", "first_name", "last_name", "passengername", "customername",
        "ticket", "id", "passengerid", "customerid", "transactionid", "uuid", "guid",
        "address", "comment", "description", "email", "url", "phone", "rowid"
    };

    public static DashboardDefinition CreateFromModel(AnalyticsModel model, Guid? dashboardId = null)
    {
        var id = dashboardId ?? Guid.NewGuid();
        var cleanModelName = model.Name.Replace(" Semantic Model", "", StringComparison.OrdinalIgnoreCase).Trim();
        var dashboardName = $"{cleanModelName} Dashboard";
        var dashboard = new DashboardDefinition(dashboardName, id);

        var primaryTable = model.Tables.FirstOrDefault();
        var tableName = primaryTable?.Name ?? "Data";

        // Collect available columns & measures
        var columns = primaryTable?.Columns.ToList() ?? new List<ModelColumn>();
        var measures = primaryTable?.Measures.ToList() ?? model.Measures.ToList();

        // Ensure TotalRows measure exists on primary table and model if missing
        if (!measures.Any(m => m.Name == "TotalRows") && primaryTable != null)
        {
            var countExpr = new MeasureExpression($"COUNTROWS('{tableName}')", "integer");
            var countMeasure = Measure.Create("TotalRows", countExpr, tableName);
            if (countMeasure.IsSuccess && countMeasure.Value != null)
            {
                primaryTable.AddMeasure(countMeasure.Value);
                model.AddMeasure(countMeasure.Value);
                measures.Add(countMeasure.Value);
            }
        }

        // Filter for meaningful categorical dimensions (avoid high-cardinality unique names/tickets)
        var categoryCols = columns
            .Where(c => !IdentifierColumnNames.Contains(c.Name))
            .Where(c => c.DataType == ColumnDataType.String ||
                        c.Name.Equals("pclass", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals("class", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals("tier", StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Sort categoryCols prioritizing well-known categorical columns
        categoryCols = categoryCols.OrderBy(c =>
        {
            var l = c.Name.ToLowerInvariant();
            if (l is "sex" or "gender") return 1;
            if (l is "pclass" or "class") return 2;
            if (l is "embarked" or "region" or "country") return 3;
            if (l is "status" or "category" or "type") return 4;
            return 10;
        }).ToList();

        var primaryCategory = categoryCols.FirstOrDefault()?.Name ?? "Category";
        var secondaryCategory = categoryCols.Skip(1).FirstOrDefault()?.Name ?? primaryCategory;

        // Measures selection
        var primaryMeasure = measures.FirstOrDefault(m => m.Name == "TotalRows")?.Name ?? "TotalRows";

        // Search for rate/percentage measure (e.g. survived_Rate, target_Rate)
        var rateMeasure = measures.FirstOrDefault(m => m.Name.EndsWith("_Rate", StringComparison.OrdinalIgnoreCase));

        // Search for financial/summary measure (e.g. Total_fare, Total_revenue, TotalRevenue, TotalSales)
        var financialMeasure = measures.FirstOrDefault(m =>
            (m.Name.StartsWith("Total", StringComparison.OrdinalIgnoreCase) || m.Name.StartsWith("Sum", StringComparison.OrdinalIgnoreCase)) &&
            !m.Name.Equals("TotalRows", StringComparison.OrdinalIgnoreCase));

        // Search for average measurement (e.g. Average_age, Average_fare, AverageRevenue)
        var averageMeasure = measures.FirstOrDefault(m => m.Name.StartsWith("Average", StringComparison.OrdinalIgnoreCase));

        var card2Measure = rateMeasure?.Name ?? (financialMeasure?.Name ?? (averageMeasure?.Name ?? primaryMeasure));
        var card3Measure = averageMeasure?.Name ?? (financialMeasure?.Name ?? primaryMeasure);

        // ==========================================
        // Page 1: Overview & Analytics
        // ==========================================
        var overviewPage = new Page("Overview & Analytics", 1280, 720);

        // KPI Card 1: Total Records
        overviewPage.AddVisual(new Visual(
            VisualTypes.Card,
            "kpi-total-records",
            new VisualLayout(40, 30, 340, 160, 1, true),
            new[] { $"{tableName}[{primaryMeasure}]" }));

        // KPI Card 2: Core Rate / Primary Domain Metric
        overviewPage.AddVisual(new Visual(
            VisualTypes.Card,
            "kpi-core-metric",
            new VisualLayout(420, 30, 340, 160, 1, true),
            new[] { $"{tableName}[{card2Measure}]" }));

        // KPI Card 3: Diagnostic Average / Secondary Metric
        overviewPage.AddVisual(new Visual(
            VisualTypes.Card,
            "kpi-diagnostic-metric",
            new VisualLayout(800, 30, 340, 160, 1, true),
            new[] { $"{tableName}[{card3Measure}]" }));

        // Visual 4: Categorical Distribution (Bar Chart with low-cardinality dimension)
        overviewPage.AddVisual(new Visual(
            VisualTypes.BarChart,
            "chart-category-distribution",
            new VisualLayout(40, 220, 720, 440, 1, true),
            new[] { $"{tableName}[{primaryCategory}]", $"{tableName}[{primaryMeasure}]" }));

        // Visual 5: Proportions / Slices (Donut Chart with secondary dimension)
        overviewPage.AddVisual(new Visual(
            VisualTypes.DonutChart,
            "chart-breakdown-donut",
            new VisualLayout(800, 220, 440, 440, 1, true),
            new[] { $"{tableName}[{secondaryCategory}]", $"{tableName}[{primaryMeasure}]" }));

        dashboard.AddPage(overviewPage);

        // ==========================================
        // Page 2: Detailed Records (Data Grid)
        // ==========================================
        var detailsPage = new Page("Detailed Records", 1280, 720);
        var tableFields = columns.Take(8).Select(c => $"{tableName}[{c.Name}]").ToList();
        if (tableFields.Count == 0)
        {
            tableFields.Add($"{tableName}[{primaryMeasure}]");
        }

        detailsPage.AddVisual(new Visual(
            VisualTypes.Table,
            "table-detailed-records",
            new VisualLayout(40, 40, 1200, 640, 1, true),
            tableFields));

        dashboard.AddPage(detailsPage);

        return dashboard;
    }
}
