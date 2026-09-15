using System.IO.Compression;
using System.Text;
using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Infrastructure.PowerBi;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public class PbipCompilationPathTests
{
    [Fact]
    public void FullPbipCompilationPipeline_EndToEndInMemoryAndZipArchive_MatchesEnterpriseSpec()
    {
        // 1. Arrange Canonical Analytics Model
        var model = new AnalyticsModel("GlobalSalesModel", "en-US");

        var customers = model.GetOrAddTable("Customers");
        customers.AddColumn(new ModelColumn("CustomerId", ColumnDataType.Int64, "CustomerId"));
        customers.AddColumn(new ModelColumn("CustomerName", ColumnDataType.String, "CustomerName"));

        var orders = model.GetOrAddTable("Orders");
        orders.AddColumn(new ModelColumn("OrderId", ColumnDataType.Int64, "OrderId"));
        orders.AddColumn(new ModelColumn("CustomerId", ColumnDataType.Int64, "CustomerId"));
        orders.AddColumn(new ModelColumn("OrderAmount", ColumnDataType.Decimal, "OrderAmount", "$#,0.00"));

        var totalSales = Measure.Create("TotalSales", new MeasureExpression("SUM(Orders[OrderAmount])", "decimal"), "Orders");
        orders.AddMeasure(totalSales.Value!);

        model.AddRelationship(new ModelRelationship(
            "Customers_Orders_Rel",
            "Orders", "CustomerId",
            "Customers", "CustomerId",
            RelationshipCrossFiltering.Single,
            isActive: true));

        // 2. Arrange Canonical Dashboard Definition
        var dashboard = new DashboardDefinition("ExecutiveSalesOverview", Guid.NewGuid());
        var page1 = new Page("SummaryPage", 1920, 1080);
        page1.AddVisual(new Visual(
            VisualTypes.BarChart,
            "SalesByCustomerChart",
            new VisualLayout(50, 50, 800, 500, 1, true),
            ["Customers[CustomerName]", "Orders[TotalSales]"]));
        dashboard.AddPage(page1);

        var page2 = new Page("DetailsPage", 1920, 1080);
        page2.AddVisual(new Visual(
            VisualTypes.Table,
            "OrdersDataTable",
            new VisualLayout(20, 20, 1200, 700, 1, true),
            ["Orders[OrderId]", "Orders[OrderAmount]"]));
        dashboard.AddPage(page2);

        // 3. Act - Compile Project
        var pbirGenerator = new PbirGenerator();
        var tmdlGenerator = new TmdlGenerator();
        var compiler = new PbipCompiler(pbirGenerator, tmdlGenerator);

        var fileTree = compiler.CompileProject("GlobalSalesDashboard", dashboard, model);

        // 4. Assert In-Memory Structure
        fileTree.Files.Should().NotBeEmpty();
        fileTree.ContainsFile("GlobalSalesDashboard.pbip").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition.pbir").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/report.json").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/pages/pages.json").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/pages/SummaryPage/page.json").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/pages/SummaryPage/visuals/SalesByCustomerChart/visual.json").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/pages/DetailsPage/page.json").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.Report/definition/pages/DetailsPage/visuals/OrdersDataTable/visual.json").Should().BeTrue();

        fileTree.ContainsFile("GlobalSalesDashboard.SemanticModel/definition.pbism").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.SemanticModel/definition/model.tmdl").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.SemanticModel/definition/relationships.tmdl").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.SemanticModel/definition/tables/Customers.tmdl").Should().BeTrue();
        fileTree.ContainsFile("GlobalSalesDashboard.SemanticModel/definition/tables/Orders.tmdl").Should().BeTrue();

        // 5. Assert ZIP Archiving
        var zipBytes = fileTree.ToZipArchive();
        zipBytes.Should().NotBeNull();
        zipBytes.Length.Should().BeGreaterThan(500);

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        archive.Entries.Should().Contain(e => e.FullName == "GlobalSalesDashboard.pbip");
        archive.Entries.Should().Contain(e => e.FullName == "GlobalSalesDashboard.Report/definition.pbir");
        archive.Entries.Should().Contain(e => e.FullName == "GlobalSalesDashboard.SemanticModel/definition/tables/Orders.tmdl");

        var pbipEntry = archive.GetEntry("GlobalSalesDashboard.pbip");
        pbipEntry.Should().NotBeNull();
        using var reader = new StreamReader(pbipEntry!.Open(), Encoding.UTF8);
        var content = reader.ReadToEnd();
        content.Should().Contain("GlobalSalesDashboard.Report");
    }
}

