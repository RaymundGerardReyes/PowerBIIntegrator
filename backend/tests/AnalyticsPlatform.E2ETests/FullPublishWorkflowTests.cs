using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AnalyticsPlatform.E2ETests;

public class FullPublishWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FullPublishWorkflowTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task PublishAndRetrieveEmbedConfig_SucceedsEndToEnd()
    {
        var publishPayload = new { DashboardDefinitionId = Guid.NewGuid(), TargetWorkspaceId = "ws-test-123" };
        var publishResponse = await _client.PostAsJsonAsync("/api/powerbi/publish", publishPayload);
        publishResponse.EnsureSuccessStatusCode();

        var embedResponse = await _client.GetAsync("/api/powerbi/embed-config/rep-123");
        embedResponse.EnsureSuccessStatusCode();
        var embedBody = await embedResponse.Content.ReadAsStringAsync();
        embedBody.Should().Contain("rep-123");
    }

    [Fact]
    public async Task CompileAndDownloadPbipProject_ExtractsValidPbipFolderStructureEndToEnd()
    {
        var projectName = "EndToEndPbipTest";
        var payload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            AnalyticsModelId = Guid.NewGuid(),
            ProjectName = projectName
        };

        var response = await _client.PostAsJsonAsync("/api/powerbi/compile-pbip/download", payload);
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        zipBytes.Should().NotBeEmpty();

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            using (var memoryStream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDir);
            }

            var pbipFilePath = Path.Combine(tempDir, $"{projectName}.pbip");
            File.Exists(pbipFilePath).Should().BeTrue();
            var pbipJson = File.ReadAllText(pbipFilePath);
            pbipJson.Should().Contain($"{projectName}.Report");

            var reportPbirPath = Path.Combine(tempDir, $"{projectName}.Report", "definition.pbir");
            File.Exists(reportPbirPath).Should().BeTrue();
            var pbirJson = File.ReadAllText(reportPbirPath);
            pbirJson.Should().Contain($"{projectName}.SemanticModel");

            var modelTmdlPath = Path.Combine(tempDir, $"{projectName}.SemanticModel", "definition", "model.tmdl");
            File.Exists(modelTmdlPath).Should().BeTrue();
            var modelTmdl = File.ReadAllText(modelTmdlPath);
            modelTmdl.Should().Contain("model Model");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
