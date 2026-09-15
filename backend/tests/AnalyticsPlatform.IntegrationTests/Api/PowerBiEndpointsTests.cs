using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class PowerBiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PowerBiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCompilePbip_WithValidPayload_ReturnsOkWithManifest()
    {
        var payload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            AnalyticsModelId = Guid.NewGuid(),
            ProjectName = "IntegrationTestProject"
        };

        var response = await _client.PostAsJsonAsync("/api/powerbi/compile-pbip", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IntegrationTestProject");
        body.Should().Contain("totalFiles");
    }

    [Fact]
    public async Task PostCompilePbipDownload_ReturnsZipStreamWithValidEntries()
    {
        var payload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            AnalyticsModelId = Guid.NewGuid(),
            ProjectName = "DownloadZipTest"
        };

        var response = await _client.PostAsJsonAsync("/api/powerbi/compile-pbip/download", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();

        using var memoryStream = new MemoryStream(bytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        archive.Entries.Should().Contain(e => e.FullName == "DownloadZipTest.pbip");
        archive.Entries.Should().Contain(e => e.FullName.StartsWith("DownloadZipTest.Report/definition"));
        archive.Entries.Should().Contain(e => e.FullName.StartsWith("DownloadZipTest.SemanticModel/definition"));
    }

    [Fact]
    public async Task PostCompilePbir_ReturnsPbirFiles()
    {
        var payload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            SemanticModelRelativePath = "../CustomModel"
        };

        var response = await _client.PostAsJsonAsync("/api/powerbi/compile-pbir", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("definition.pbir");
    }

    [Fact]
    public async Task PostCompileTmdl_ReturnsTmdlFiles()
    {
        var payload = new
        {
            AnalyticsModelId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/api/powerbi/compile-tmdl", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("definition/model.tmdl");
    }
}

