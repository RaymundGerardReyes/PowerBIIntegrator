using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Rules;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.DataQuality;

public class VisualMappingRuleTests
{
    [Fact]
    public void MapSuggestions_WhenDateTimeAndNumericPresent_RecommendsLineChart()
    {
        var profile = new DatasetProfile("TimeSeries", "source.csv", 50);
        profile.AddColumnProfile(new ColumnProfile("OrderDate", "DateTime", 50, 0, 0.0, 50, "2026-01-01", "2026-09-16", new[] { "2026-09-16" }, @"^\d{4}-\d{2}-\d{2}$", "High"));
        profile.AddColumnProfile(new ColumnProfile("Revenue", "Decimal", 50, 0, 0.0, 45, "10.00", "5000.00", new[] { "100.00" }, @"^\d+$", "High"));

        var suggestions = VisualMappingRule.MapSuggestions(profile);

        suggestions.Should().Contain(s => s.RecommendedVisualType == "lineChart");
    }

    [Fact]
    public void MapSuggestions_WhenCategoryAndNumericPresent_RecommendsBarChart()
    {
        var profile = new DatasetProfile("Categorical", "source.csv", 50);
        profile.AddColumnProfile(new ColumnProfile("Region", "String", 50, 0, 0.0, 3, "APAC", "US", new[] { "APAC" }, @"^[A-Z]+$", "Low"));
        profile.AddColumnProfile(new ColumnProfile("SalesCount", "Int64", 50, 0, 0.0, 30, "1", "100", new[] { "10" }, @"^\d+$", "High"));

        var suggestions = VisualMappingRule.MapSuggestions(profile);

        suggestions.Should().Contain(s => s.RecommendedVisualType == "barChart");
    }
}

