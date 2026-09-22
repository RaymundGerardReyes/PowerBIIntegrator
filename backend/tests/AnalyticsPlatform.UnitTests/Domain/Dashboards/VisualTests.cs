using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.Dashboards;

public class VisualTests
{
    [Theory]
    [InlineData("TotalRevenue", true)]
    [InlineData("Total_Sales", true)]
    [InlineData("Sum_Quantity", true)]
    [InlineData("SumAmount", true)]
    [InlineData("AveragePrice", true)]
    [InlineData("Average_Fare", true)]
    [InlineData("AvgDiscount", true)]
    [InlineData("TotalRows", true)]
    [InlineData("total_rows", true)]
    [InlineData("TotalRows_Primary", true)]
    [InlineData("churn_rate", true)]
    [InlineData("SurvivalRate", true)]
    [InlineData("growth_pct", true)]
    [InlineData("margin_percentage", true)]
    [InlineData("Revenue", false)]
    [InlineData("Amount", false)]
    [InlineData("Price", false)]
    [InlineData("CustomerId", false)]
    [InlineData("CustomerName", false)]
    [InlineData("OrderDate", false)]
    public void IsMeasureName_ValidatesCorrectMeasureTaxonomy(string name, bool expected)
    {
        Visual.IsMeasureName(name).Should().Be(expected);
    }

    [Fact]
    public void VisualConstructor_ForTable_MapsAllFieldsToValuesRole()
    {
        var visual = new Visual(
            VisualTypes.Table,
            "DetailsTable",
            new VisualLayout(0, 0, 800, 400, 1, true),
            new[] { "Orders[OrderId]", "Orders[CustomerName]", "Orders[TotalRevenue]" });

        visual.QueryBinding.Should().NotBeNull();
        visual.QueryBinding!.Categories.Should().BeEmpty();
        visual.QueryBinding!.Values.Should().HaveCount(3);
        visual.QueryBinding!.Values[0].Field.Should().Be("OrderId");
        visual.QueryBinding!.Values[0].IsMeasure.Should().BeFalse();
        visual.QueryBinding!.Values[2].Field.Should().Be("TotalRevenue");
        visual.QueryBinding!.Values[2].IsMeasure.Should().BeTrue();
    }

    [Fact]
    public void VisualConstructor_ForCard_MapsScalarMeasureToValuesRole()
    {
        var visual = new Visual(
            VisualTypes.Card,
            "RevenueCard",
            new VisualLayout(0, 0, 300, 180, 1, true),
            new[] { "Sales[TotalRevenue]" });

        visual.QueryBinding.Should().NotBeNull();
        visual.QueryBinding!.Categories.Should().BeEmpty();
        visual.QueryBinding!.Values.Should().HaveCount(1);
        visual.QueryBinding!.Values[0].Field.Should().Be("TotalRevenue");
        visual.QueryBinding!.Values[0].IsMeasure.Should().BeTrue();
    }

    [Fact]
    public void VisualConstructor_ForBarChart_PartitionsCategoriesAndValues()
    {
        var visual = new Visual(
            VisualTypes.BarChart,
            "SalesByRegion",
            new VisualLayout(0, 0, 600, 300, 1, true),
            new[] { "Sales[Region]", "Sales[TotalRevenue]" });

        visual.QueryBinding.Should().NotBeNull();
        visual.QueryBinding!.Categories.Should().HaveCount(1);
        visual.QueryBinding!.Categories[0].Field.Should().Be("Region");
        visual.QueryBinding!.Categories[0].IsMeasure.Should().BeFalse();

        visual.QueryBinding!.Values.Should().HaveCount(1);
        visual.QueryBinding!.Values[0].Field.Should().Be("TotalRevenue");
        visual.QueryBinding!.Values[0].IsMeasure.Should().BeTrue();
    }
}
