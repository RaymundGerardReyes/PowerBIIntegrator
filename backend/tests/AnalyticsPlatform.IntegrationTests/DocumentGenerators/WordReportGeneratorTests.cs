using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Word;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.DocumentGenerators;

public class WordReportGeneratorTests
{
    private readonly WordReportGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_ProducesValidWordprocessingDocument()
    {
        var model = new ReportDocumentModel(
            Title: "Executive Briefing",
            Subtitle: "Strategic Initiatives FY2026",
            Author: "Strategy Office",
            Organization: "Acme Global",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Key Milestones",
                    Narrative: "Platform modernization completed on schedule.",
                    Kpis: new List<ReportKpiDto>
                    {
                        new("Uptime SLA", "99.99%", "+0.05%", "30-day trailing")
                    },
                    TableHeaders: new List<string> { "Initiative", "Owner", "Status" },
                    TableRows: new List<IReadOnlyList<string>>
                    {
                        new List<string> { "PBIR Migration", "Engineering", "Completed" },
                        new List<string> { "Power BI REST Sync", "Infra", "In Progress" }
                    })
            });

        var bytes = await _generator.GenerateAsync(model);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // Verify valid OpenXML docx package
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        wordDoc.MainDocumentPart.Should().NotBeNull();
        var body = wordDoc.MainDocumentPart!.Document.Body;
        body.Should().NotBeNull();

        var text = body!.InnerText;
        text.Should().Contain("Executive Briefing");
        text.Should().Contain("Strategic Initiatives FY2026");
        text.Should().Contain("Key Milestones");
        text.Should().Contain("PBIR Migration");

        var tables = body.Elements<Table>().ToList();
        tables.Should().NotBeEmpty();
    }
}
