using FluentAssertions;
using Xunit;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.Rules;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;

namespace AnalyticsPlatform.UnitTests.Domain.Analytics;

public class ValidateAnalyticsModelRulesTests
{
    [Fact]
    public void Validate_WithValidModelAndRelationships_ReturnsValidReport()
    {
        var model = new AnalyticsModel("SalesAnalytics");
        var orders = model.GetOrAddTable("Orders");
        var customers = model.GetOrAddTable("Customers");

        orders.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));
        orders.AddColumn(new ModelColumn("CustomerId", ColumnDataType.Int64, "CustomerId"));
        customers.AddColumn(new ModelColumn("Id", ColumnDataType.Int64, "Id"));

        model.AddRelationship(new ModelRelationship("FK_Orders_Customers", "Orders", "CustomerId", "Customers", "Id"));
        var measure = Measure.Create("TotalSales", new MeasureExpression("SUM(Orders[Amount])", "decimal"), "Orders").Value!;
        model.AddMeasure(measure);

        var report = ValidateAnalyticsModelRules.Validate(model);

        report.IsValid.Should().BeTrue();
        report.Errors.Should().BeEmpty();
        report.OrphanTables.Should().BeEmpty();
        report.DetectedCycles.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithOrphanTables_FlagsWarningAndOrphanList()
    {
        var model = new AnalyticsModel("ModelWithOrphan");
        model.GetOrAddTable("Orders");
        model.GetOrAddTable("Customers");
        model.GetOrAddTable("UnlinkedLookup");

        model.AddRelationship(new ModelRelationship("Rel_Orders_Customers", "Orders", "CustomerId", "Customers", "Id"));

        var report = ValidateAnalyticsModelRules.Validate(model);

        report.IsValid.Should().BeTrue();
        report.OrphanTables.Should().Contain("UnlinkedLookup");
        report.Warnings.Should().Contain(w => w.Contains("UnlinkedLookup"));
    }

    [Fact]
    public void Validate_WithCircularRelationships_FlagsErrorAndIdentifiesCycle()
    {
        var model = new AnalyticsModel("CyclicModel");
        model.GetOrAddTable("TableA");
        model.GetOrAddTable("TableB");
        model.GetOrAddTable("TableC");

        // Cycle: A -> B -> C -> A
        model.AddRelationship(new ModelRelationship("Rel_A_B", "TableA", "Col1", "TableB", "Col1"));
        model.AddRelationship(new ModelRelationship("Rel_B_C", "TableB", "Col2", "TableC", "Col2"));
        model.AddRelationship(new ModelRelationship("Rel_C_A", "TableC", "Col3", "TableA", "Col3"));

        var report = ValidateAnalyticsModelRules.Validate(model);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Contains("Circular relationship loop detected"));
        report.DetectedCycles.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyOrUnsafeMeasure_FlagsError()
    {
        var model = new AnalyticsModel("UnsafeMeasureModel");
        var table = model.GetOrAddTable("Table1");
        table.AddColumn(new ModelColumn("Col1", ColumnDataType.String, "Col1"));

        var malicious = Measure.Create("MaliciousMeasure", new MeasureExpression("DROP TABLE Customers;", "string"), "Table1").Value!;
        model.AddMeasure(malicious);

        var report = ValidateAnalyticsModelRules.Validate(model);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Contains("dangerous or forbidden tokens"));
    }
}
