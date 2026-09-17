using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Infrastructure.PowerBi;
using Xunit;

namespace AnalyticsPlatform.RegressionTests;

public class PbirGenerationRegressionTests
{
    private static readonly string[] BoundFields = ["Sales[TotalRevenue]", "Sales[Region]"];

    [Fact]
    public void GeneratePageJson_ProducesValidLayoutContract()
    {
        var page = new Page("ExecutiveOverview", 1920, 1080);
        page.AddVisual(new Visual("kpi", "RevenueKpi",
            new VisualLayout(40, 30, 400, 180, 0, true), BoundFields));

        var generator = new PbirGenerator();
        var json = generator.GeneratePageJson(page);

        json.Should().Contain("ExecutiveOverview");
        json.Should().Contain("RevenueKpi");
        json.Should().Contain("Sales[TotalRevenue]");
    }

    [Fact]
    public void GenerateReportDefinition_ProducesValidPbirEnhancedSchemaAndFolderTree()
    {
        var dashboard = new DashboardDefinition("CorporatePerformance", Guid.NewGuid());
        var page = new Page("RevenueSummary", 1280, 720);
        page.AddVisual(new Visual(
            VisualTypes.BarChart,
            "RegionalRevenueBar",
            new VisualLayout(20, 20, 600, 340, 1, true),
            BoundFields));
        dashboard.AddPage(page);

        var generator = new PbirGenerator();
        var fileTree = generator.GenerateReportDefinition(dashboard, "../CorporatePerformance.SemanticModel");

        fileTree.ContainsFile("definition.pbir").Should().BeTrue();
        fileTree.ContainsFile("definition/version.json").Should().BeTrue();
        fileTree.ContainsFile("definition/report.json").Should().BeTrue();
        fileTree.ContainsFile("definition/pages/pages.json").Should().BeTrue();
        fileTree.ContainsFile("definition/pages/RevenueSummary/page.json").Should().BeTrue();
        fileTree.ContainsFile("definition/pages/RevenueSummary/visuals/RegionalRevenueBar/visual.json").Should().BeTrue();

        var versionJson = fileTree.GetContent("definition/version.json");
        versionJson.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json\"");
        versionJson.Should().NotContain("\"schema\":");
        versionJson.Should().Contain("\"version\": \"2.0.0\"");

        var pbir = fileTree.GetContent("definition.pbir");
        pbir.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json\"");
        pbir.Should().NotContain("\"schema\":");
        pbir.Should().Contain("byPath");
        pbir.Should().Contain("../CorporatePerformance.SemanticModel");

        var reportJson = fileTree.GetContent("definition/report.json");
        reportJson.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json\"");
        reportJson.Should().NotContain("\"schema\":");

        var pagesJson = fileTree.GetContent("definition/pages/pages.json");
        pagesJson.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json\"");
        pagesJson.Should().NotContain("\"schema\":");

        var pageJson = fileTree.GetContent("definition/pages/RevenueSummary/page.json");
        pageJson.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.1.0/schema.json\"");
        pageJson.Should().NotContain("\"schema\":");
        pageJson.Should().Contain("displayName");
        pageJson.Should().Contain("displayOption");

        var visual = fileTree.GetContent("definition/pages/RevenueSummary/visuals/RegionalRevenueBar/visual.json");
        visual.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.2.0/schema.json\"");
        visual.Should().NotContain("\"schema\":");
        visual.Should().Contain(VisualTypes.BarChart);
        visual.Should().Contain("queryState");
        visual.Should().Contain("Category");
        visual.Should().Contain("Y");
    }

    [Fact]
    public void GenerateSemanticModel_ProducesValidTmdlStructureAndPartitions()
    {
        var model = new AnalyticsModel("SalesModel", "en-US");
        var table = model.GetOrAddTable("FactSales");
        table.AddColumn(new ModelColumn("TransactionId", ColumnDataType.Int64, "TransactionId"));
        table.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        table.AddColumn(new ModelColumn("Revenue", ColumnDataType.Decimal, "Revenue", "$#,0.00"));

        var measure = Measure.Create("TotalSales", new MeasureExpression("SUM(FactSales[Revenue])", "decimal"), "FactSales");
        table.AddMeasure(measure.Value!);

        var generator = new TmdlGenerator();
        var fileTree = generator.GenerateSemanticModel(model);

        fileTree.ContainsFile("definition.pbism").Should().BeTrue();
        fileTree.ContainsFile("definition/model.tmdl").Should().BeTrue();
        fileTree.ContainsFile("definition/tables/FactSales.tmdl").Should().BeTrue();

        var pbismContent = fileTree.GetContent("definition.pbism");
        pbismContent.Should().Contain("\"$schema\": \"https://developer.microsoft.com/json-schemas/fabric/item/semanticModel/definitionProperties/1.0.0/schema.json\"");
        pbismContent.Should().NotContain("\"schema\":");

        var modelContent = fileTree.GetContent("definition/model.tmdl");
        modelContent.Should().Contain("ref table 'FactSales'");
        modelContent.Should().Contain("culture: en-US");

        var tableContent = fileTree.GetContent("definition/tables/FactSales.tmdl");
        tableContent.Should().Contain("table 'FactSales'");
        tableContent.Should().Contain("column 'TransactionId'");
        tableContent.Should().Contain("dataType: int64");
        tableContent.Should().Contain("measure 'TotalSales' = SUM(FactSales[Revenue])");
        tableContent.Should().Contain("partition 'FactSales-Partition' = m");
    }
}
