using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using Xunit;

namespace AnalyticsPlatform.E2ETests;

public class DocumentGenerationE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DocumentGenerationE2ETests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullDocumentGenerationSuite_EmitsValidPdfExcelAndWordFiles_EndToEnd()
    {
        var model = new ReportDocumentModel(
            Title: "Quarterly Executive Operational Briefing",
            Subtitle: "Multi-Target Document Engine E2E Verification",
            Author: "Chief Architect",
            Organization: "Contoso Enterprise Solutions",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Infrastructure & SLA Performance",
                    Narrative: "All system telemetry meets tier-1 operational standards.",
                    Kpis: new List<ReportKpiDto>
                    {
                        new("Global Uptime", "99.99%", "+0.02%", "30-day trailing"),
                        new("P99 Latency", "42ms", "-15%", "API gateway response")
                    },
                    TableHeaders: new List<string> { "Service", "Region", "Status", "Latency" },
                    TableRows: new List<IReadOnlyList<string>>
                    {
                        new List<string> { "PBIR Compiler", "East US", "Healthy", "28ms" },
                        new List<string> { "Document Engine", "West Europe", "Healthy", "35ms" }
                    })
            });

        // 1. PDF Endpoint E2E
        var pdfResponse = await _client.PostAsJsonAsync("/api/reports/pdf", model);
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        pdfBytes.Should().NotBeNull();
        Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray()).Should().Be("%PDF-");

        // 2. Excel Endpoint E2E
        var excelResponse = await _client.PostAsJsonAsync("/api/reports/excel", model);
        excelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        excelResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var excelBytes = await excelResponse.Content.ReadAsByteArrayAsync();
        excelBytes.Should().NotBeNull();
        excelBytes[0].Should().Be(0x50); // PK\x03\x04
        excelBytes[1].Should().Be(0x4B);

        // 3. Word Endpoint E2E
        var wordResponse = await _client.PostAsJsonAsync("/api/reports/word", model);
        wordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        wordResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        var wordBytes = await wordResponse.Content.ReadAsByteArrayAsync();
        wordBytes.Should().NotBeNull();
        wordBytes[0].Should().Be(0x50); // PK\x03\x04
        wordBytes[1].Should().Be(0x4B);
    }
}
