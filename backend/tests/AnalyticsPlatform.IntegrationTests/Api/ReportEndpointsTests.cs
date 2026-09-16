using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class ReportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static ReportDocumentModel CreateSampleReportModel(string title) => new(
        Title: title,
        Subtitle: "Monthly Performance Assessment",
        Author: "Analytics Engine",
        Organization: "Enterprise BI Corp",
        Sections: new List<ReportSectionDto>
        {
            new(
                Title: "Revenue & Margins",
                Narrative: "Strong sales across all regions with margin expansion.",
                Kpis: new List<ReportKpiDto>
                {
                    new("Net Sales", "$14.5M", "+12.4%", "Net booked revenue"),
                    new("Operating Margin", "28.5%", "+2.1%", "Adjusted EBITDA margin")
                },
                TableHeaders: new List<string> { "Segment", "Revenue", "YoY" },
                TableRows: new List<IReadOnlyList<string>>
                {
                    new List<string> { "Enterprise", "$9.2M", "+18%" },
                    new List<string> { "Mid-Market", "$5.3M", "+4%" }
                })
        });

    [Fact]
    public async Task PostGeneratePdf_ReturnsOkWithPdfStreamAndValidMagicBytes()
    {
        var model = CreateSampleReportModel("Executive Briefing PDF");

        var response = await _client.PostAsJsonAsync("/api/reports/pdf", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task PostGenerateExcel_ReturnsOkWithExcelStreamAndZipSignature()
    {
        var model = CreateSampleReportModel("Executive Summary Excel");

        var response = await _client.PostAsJsonAsync("/api/reports/excel", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // OpenXML xlsx is a ZIP package starting with PK\x03\x04
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
        bytes[2].Should().Be(0x03);
        bytes[3].Should().Be(0x04);
    }

    [Fact]
    public async Task PostGenerateWord_ReturnsOkWithWordStreamAndZipSignature()
    {
        var model = CreateSampleReportModel("Executive Memo Word");

        var response = await _client.PostAsJsonAsync("/api/reports/word", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // OpenXML docx is a ZIP package starting with PK\x03\x04
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
        bytes[2].Should().Be(0x03);
        bytes[3].Should().Be(0x04);
    }

    [Fact]
    public async Task PostGeneratePdf_WithEmptyTitle_ReturnsBadRequest()
    {
        var model = new ReportDocumentModel(string.Empty);

        var response = await _client.PostAsJsonAsync("/api/reports/pdf", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
