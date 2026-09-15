using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.Analytics;

public class MeasureTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsSuccess()
    {
        var expression = new MeasureExpression("SUM(Sales[Amount])", "decimal");
        var result = Measure.Create("TotalRevenue", expression, "Sales");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TotalRevenue");
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFailure()
    {
        var expression = new MeasureExpression("SUM(Sales[Amount])", "decimal");
        var result = Measure.Create("", expression, "Sales");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name"));
    }
}
