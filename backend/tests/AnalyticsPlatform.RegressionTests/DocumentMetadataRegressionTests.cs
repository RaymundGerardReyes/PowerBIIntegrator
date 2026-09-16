using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Excel;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Pdf;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Word;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.RegressionTests;

public class DocumentMetadataRegressionTests
{
    private readonly PdfReportGenerator _pdfGen = new();
    private readonly ExcelReportGenerator _excelGen = new();
    private readonly WordReportGenerator _wordGen = new();

    private static ReportDocumentModel CreateDeterministicModel() => new(
        Title: "Deterministic Enterprise Report",
        Subtitle: "Regression Baseline Snapshot",
        Author: "Golden Test Harness",
        Organization: "Enterprise Holding Corp",
        GeneratedAt: new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
        Sections: new List<ReportSectionDto>
        {
            new(
                Title: "Core Operations",
                Narrative: "Deterministic operational metrics snapshot.",
                Kpis: new List<ReportKpiDto>
                {
                    new("Target Metric A", "100%", "0.0%", "Static baseline"),
                    new("Target Metric B", "$500k", "+5.0%", "Static baseline")
                },
                TableHeaders: new List<string> { "Metric", "Target", "Actual" },
                TableRows: new List<IReadOnlyList<string>>
                {
                    new List<string> { "Efficiency", "95%", "97%" },
                    new List<string> { "Quality", "99.9%", "99.95%" }
                })
        });

    [Fact]
    public async Task PdfGenerator_PreservesHeaderStructureAndMagicSignature()
    {
        var model = CreateDeterministicModel();
        var bytes = await _pdfGen.GenerateAsync(model);

        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(500);

        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task ExcelGenerator_PreservesWorksheetsAndStructureAcrossSnapshots()
    {
        var model = CreateDeterministicModel();
        var bytes = await _excelGen.GenerateAsync(model);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);

        workbook.Worksheets.Count.Should().Be(2);
        var summaryWs = workbook.Worksheet(1);
        summaryWs.Name.Should().Be("Executive Summary");
        summaryWs.Cell("A1").GetString().Should().Be("Deterministic Enterprise Report");
        summaryWs.Cell("B4").GetString().Should().Be("Enterprise Holding Corp");
        summaryWs.Cell("B5").GetString().Should().Be("Golden Test Harness");

        var dataWs = workbook.Worksheet(2);
        dataWs.Name.Should().Be("Data Tables");
        dataWs.Cell("A1").GetString().Should().Be("Core Operations");
        dataWs.Cell("A3").GetString().Should().Be("Metric");
        dataWs.Cell("B3").GetString().Should().Be("Target");
        dataWs.Cell("C3").GetString().Should().Be("Actual");
    }

    [Fact]
    public async Task WordGenerator_PreservesMainDocumentStructureAndRuns()
    {
        var model = CreateDeterministicModel();
        var bytes = await _wordGen.GenerateAsync(model);

        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        wordDoc.MainDocumentPart.Should().NotBeNull();
        var text = wordDoc.MainDocumentPart!.Document.Body!.InnerText;

        text.Should().Contain("Deterministic Enterprise Report");
        text.Should().Contain("Regression Baseline Snapshot");
        text.Should().Contain("Enterprise Holding Corp");
        text.Should().Contain("Core Operations");
        text.Should().Contain("Target Metric A");
    }
}
