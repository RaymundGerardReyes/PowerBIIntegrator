using FluentAssertions;
using NetArchTest.Rules;
using AnalyticsPlatform.Domain.Features.ReportGeneration.Rules;
using Xunit;

namespace AnalyticsPlatform.SecurityTests;

public class ReportSecurityTests
{
    [Fact]
    public void Domain_Should_Not_DependOn_DocumentGenerationLibraries()
    {
        var result = Types.InAssembly(typeof(AnalyticsPlatform.Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "QuestPDF",
                "ClosedXML",
                "DocumentFormat.OpenXml")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData("=cmd|'/C calc'!A0")]
    [InlineData("+1234")]
    [InlineData("-5678")]
    [InlineData("@SUM(A1:B10)")]
    [InlineData("\tmalicious_tab")]
    [InlineData("\rmalicious_cr")]
    public void ExcelFormulaInjection_LeadingDangerousChars_AreSanitizedWithQuotePrefix(string dangerousInput)
    {
        var sanitized = ReportGenerationRules.SanitizeCellForFormulaInjection(dangerousInput);

        sanitized.Should().StartWith("'");
        sanitized.Substring(1).Should().Be(dangerousInput);
    }

    [Fact]
    public void ReportSectionCount_ExceedingLimit_IsRejectedToPreventDos()
    {
        var validation = ReportGenerationRules.ValidateSectionCount(101);

        validation.IsSuccess.Should().BeFalse();
        validation.Errors.Should().Contain("A report cannot contain more than 100 sections.");
    }
}
