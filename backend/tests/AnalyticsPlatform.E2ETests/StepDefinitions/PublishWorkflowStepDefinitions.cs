using System.IO.Compression;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace AnalyticsPlatform.E2ETests.StepDefinitions;

[Binding]
public class PublishWorkflowStepDefinitions : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private object? _requestPayload;
    private HttpResponseMessage? _response;
    private byte[]? _zipBytes;
    private bool _disposed;

    public PublishWorkflowStepDefinitions()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Given("an analytics model with valid tables and relationships")]
    public void GivenAnAnalyticsModelWithValidTablesAndRelationships()
    {
        _requestPayload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            AnalyticsModelId = Guid.NewGuid(),
            ProjectName = "BddPublishTest"
        };
    }

    [When("I request to compile and download the PBIP archive")]
    public async Task WhenIRequestToCompileAndDownloadThePbipArchive()
    {
        _response = await _client.PostAsJsonAsync("/api/powerbi/compile-pbip/download", _requestPayload);
        _response.EnsureSuccessStatusCode();
        _zipBytes = await _response.Content.ReadAsByteArrayAsync();
    }

    [Then("the response should contain a valid zip archive")]
    public void ThenTheResponseShouldContainAValidZipArchive()
    {
        _response.Should().NotBeNull();
        _response!.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        _zipBytes.Should().NotBeNullOrEmpty();
    }

    [Then("the zip archive should contain PBIR and TMDL definitions")]
    public void ThenTheZipArchiveShouldContainPbirAndTmdlDefinitions()
    {
        using var memoryStream = new MemoryStream(_zipBytes!);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        archive.Entries.Should().Contain(e => e.FullName.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase));
        archive.Entries.Should().Contain(e => e.FullName.EndsWith("definition.pbir", StringComparison.OrdinalIgnoreCase));
        archive.Entries.Should().Contain(e => e.FullName.EndsWith("model.tmdl", StringComparison.OrdinalIgnoreCase));
    }

    [Given("a dashboard definition with visuals and layout bounds")]
    public void GivenADashboardDefinitionWithVisualsAndLayoutBounds()
    {
        _requestPayload = new
        {
            DashboardDefinitionId = Guid.NewGuid(),
            TargetWorkspaceId = "ws-bdd-test"
        };
    }

    [When("I publish the dashboard to Fabric workspace")]
    public async Task WhenIPublishTheDashboardToFabricWorkspace()
    {
        _response = await _client.PostAsJsonAsync("/api/powerbi/publish", _requestPayload);
        _response.EnsureSuccessStatusCode();
    }

    [Then("the embed configuration should be retrievable")]
    public async Task ThenTheEmbedConfigurationShouldBeRetrievable()
    {
        _response = await _client.GetAsync("/api/powerbi/embed-config/rep-123");
        _response.EnsureSuccessStatusCode();
    }

    [Then("the embed config should contain a valid reportId and embedUrl")]
    public async Task ThenTheEmbedConfigShouldContainAValidReportIdAndEmbedUrl()
    {
        var body = await _response!.Content.ReadAsStringAsync();
        body.Should().Contain("rep-123");
        body.Should().Contain("app.powerbi.com");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client.Dispose();
                _factory.Dispose();
            }
            _disposed = true;
        }
    }
}

