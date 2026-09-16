using System.Text;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Pdf;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.DocumentGenerators;

public class PdfReportGeneratorTests
{
    private readonly PdfReportGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_ProducesValidPdfDocumentWithMagicBytes()
    {
        var model = new ReportDocumentModel(
            Title: "Board of Directors Report",
            Subtitle: "Quarterly Enterprise Summary",
            Author: "Chief Analytics Officer",
            Organization: "Enterprise Holdings",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Financial Highlights",
                    Narrative: "Net profit exceeded forecasts by 12.3% across all operating segments.",
                    Kpis: new List<ReportKpiDto>
                    {
                        new("EBITDA", "$18.2M", "+12.3%", "Operating profit"),
                        new("Gross Margin", "64.8%", "+1.5%", "Gross profit margin")
                    },
                    TableHeaders: new List<string> { "Division", "Revenue", "Margin", "Growth" },
                    TableRows: new List<IReadOnlyList<string>>
                    {
                        new List<string> { "Cloud Services", "$8.5M", "72%", "+24%" },
                        new List<string> { "Data Analytics", "$9.7M", "58%", "+10%" }
                    })
            });

        var bytes = await _generator.GenerateAsync(model);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // Check PDF magic header %PDF-
        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }
}
