using ClosedXML.Excel;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Excel;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.DocumentGenerators;

public class ExcelReportGeneratorTests
{
    private readonly ExcelReportGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_ProducesValidExcelWorkbookWithWorksheets()
    {
        var model = new ReportDocumentModel(
            Title: "Sales Performance Report",
            Subtitle: "Q3 Fiscal Overview",
            Author: "Finance Analytics",
            Organization: "Contoso Ltd",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Quarterly Revenue",
                    Narrative: "Strong sales growth across all regions.",
                    Kpis: new List<ReportKpiDto>
                    {
                        new("Total ARR", "$42.5M", "+14.2%", "Annual Recurring Revenue"),
                        new("Churn Rate", "1.2%", "-0.4%", "Net MRR Churn")
                    },
                    TableHeaders: new List<string> { "Region", "Q1", "Q2", "Q3" },
                    TableRows: new List<IReadOnlyList<string>>
                    {
                        new List<string> { "North America", "1200000", "1450000", "1600000" },
                        new List<string> { "Europe", "800000", "920000", "1100000" }
                    })
            });

        var bytes = await _generator.GenerateAsync(model);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // Verify valid OpenXML spreadsheet by opening with ClosedXML
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);

        workbook.Worksheets.Count.Should().Be(2);
        workbook.Worksheet("Executive Summary").Should().NotBeNull();
        workbook.Worksheet("Data Tables").Should().NotBeNull();

        var summaryWs = workbook.Worksheet("Executive Summary");
        summaryWs.Cell("A1").GetString().Should().Be("Sales Performance Report");
        summaryWs.Cell("B4").GetString().Should().Be("Contoso Ltd");

        var dataWs = workbook.Worksheet("Data Tables");
        dataWs.Cell("A1").GetString().Should().Be("Quarterly Revenue");
        dataWs.Cell("A3").GetString().Should().Be("Region");
    }

    [Fact]
    public async Task GenerateAsync_WithFormulaInjectionAttempt_SanitizesCells()
    {
        var model = new ReportDocumentModel(
            Title: "Security Audit",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Suspicious Payload Table",
                    TableHeaders: new List<string> { "Input", "Status" },
                    TableRows: new List<IReadOnlyList<string>>
                    {
                        new List<string> { "=cmd|' /C calc'!A0", "Blocked" },
                        new List<string> { "+SUM(A1:A10)", "Sanitized" }
                    })
            });

        var bytes = await _generator.GenerateAsync(model);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var dataWs = workbook.Worksheet("Data Tables");

        var cell1 = dataWs.Cell("A4");
        cell1.HasFormula.Should().BeFalse();
        cell1.DataType.Should().Be(XLDataType.Text);

        var cell2 = dataWs.Cell("A5");
        cell2.HasFormula.Should().BeFalse();
        cell2.DataType.Should().Be(XLDataType.Text);
    }
}
