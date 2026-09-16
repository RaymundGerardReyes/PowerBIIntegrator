using AnalyticsPlatform.Domain.Features.ReportGeneration.Entities;
using AnalyticsPlatform.Domain.Features.ReportGeneration.Rules;
using AnalyticsPlatform.Domain.Features.ReportGeneration.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.ReportGeneration;

public class ReportGenerationRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTitle_WhenNullOrEmpty_ReturnsFailure(string? title)
    {
        var result = ReportGenerationRules.ValidateTitle(title);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Report title is required.");
    }

    [Fact]
    public void ValidateTitle_WhenTooLong_ReturnsFailure()
    {
        var longTitle = new string('A', 201);
        var result = ReportGenerationRules.ValidateTitle(longTitle);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Report title cannot exceed 200 characters.");
    }

    [Fact]
    public void ValidateTitle_WhenValid_ReturnsSuccess()
    {
        var result = ReportGenerationRules.ValidateTitle("Q3 Executive Financial Summary");

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateSectionCount_WhenZeroOrNegative_ReturnsFailure(int count)
    {
        var result = ReportGenerationRules.ValidateSectionCount(count);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("A report must contain at least one section.");
    }

    [Fact]
    public void ValidateSectionCount_WhenExceedsMax_ReturnsFailure()
    {
        var result = ReportGenerationRules.ValidateSectionCount(101);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("A report cannot contain more than 100 sections.");
    }

    [Fact]
    public void ValidateTableDimensions_WhenRowsSpecifiedWithoutHeaders_ReturnsFailure()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Data1", "Data2" }
        };

        var result = ReportGenerationRules.ValidateTableDimensions(null, rows);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Table rows cannot be specified without table headers.");
    }

    [Fact]
    public void ValidateTableDimensions_WhenRowLengthMismatchesHeaders_ReturnsFailure()
    {
        var headers = new List<string> { "Col1", "Col2", "Col3" };
        var rows = new List<IReadOnlyList<string>>
        {
            new List<string> { "Val1", "Val2" }
        };

        var result = ReportGenerationRules.ValidateTableDimensions(headers, rows);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("Row 1 column count (2) does not match header count (3).");
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+SUM(A1:A10)", "'+SUM(A1:A10)")]
    [InlineData("-2+5", "'-2+5")]
    [InlineData("@SUM(B1:B5)", "'@SUM(B1:B5)")]
    [InlineData("Normal Value", "Normal Value")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void SanitizeCellForFormulaInjection_ProperlyEscapesFormulaCharacters(string? input, string expected)
    {
        var sanitized = ReportGenerationRules.SanitizeCellForFormulaInjection(input);

        sanitized.Should().Be(expected);
    }

    [Fact]
    public void ReportMetadata_WhenValid_InstantiatesSuccessfully()
    {
        var meta = new ReportMetadata("Executive Report", "FY2026 Overview", "Lead Architect", "Acme Analytics");

        meta.Title.Should().Be("Executive Report");
        meta.Subtitle.Should().Be("FY2026 Overview");
        meta.Author.Should().Be("Lead Architect");
        meta.Organization.Should().Be("Acme Analytics");
        meta.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void ReportSection_WithKpisAndTable_InstantiatesSuccessfully()
    {
        var kpis = new List<ReportKpiSummary>
        {
            new("Revenue", "$12.4M", "+8.5%", "YoY growth")
        };
        var headers = new List<string> { "Month", "Revenue" };
        var rows = new List<List<string>>
        {
            new() { "Jan", "$1.2M" },
            new() { "Feb", "$1.4M" }
        };

        var section = new ReportSection("Financial Performance", "Strong quarterly performance", kpis, headers, rows);

        section.Title.Should().Be("Financial Performance");
        section.Narrative.Should().Be("Strong quarterly performance");
        section.Kpis.Should().HaveCount(1);
        section.TableHeaders.Should().HaveCount(2);
        section.TableRows.Should().HaveCount(2);
    }
}
