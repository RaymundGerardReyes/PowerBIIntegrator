using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class DataQualityEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly string _tempCsvPath;

    public DataQualityEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _tempCsvPath = Path.Combine(Path.GetTempPath(), $"dq_integration_{Guid.NewGuid():N}.csv");

        // Prepare test data with duplicates, currency formatting, and whitespace
        var csvContent =
            "EmpId,Name,Department,Cost,Status\n" +
            "001,  John Doe  ,IT,₱1,500,ACTIVE\n" +
            "002,Mary Smith,HR,$2,000.50,active\n" +
            "001,  John Doe  ,IT,₱1,500,ACTIVE\n" +  // Exact duplicate row
            "003,Peter Jones,Finance,,N/A\n";       // Empty cost (raw null)

        File.WriteAllText(_tempCsvPath, csvContent);
    }

    public void Dispose()
    {
        if (File.Exists(_tempCsvPath))
        {
            try { File.Delete(_tempCsvPath); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ProfileEndpoint_WithValidCsv_ReturnsCompleteProfile()
    {
        var request = new
        {
            SourceReference = _tempCsvPath,
            DatasetName = "EmployeesCatalog"
        };

        var response = await _client.PostAsJsonAsync("/api/data-quality/profile", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<DatasetProfile>(JsonOptions);
        profile.Should().NotBeNull();
        profile!.DatasetName.Should().Be("EmployeesCatalog");
        profile.TotalRows.Should().Be(4);
        profile.ColumnProfiles.Should().HaveCount(5);

        var costProfile = profile.ColumnProfiles.First(c => c.ColumnName == "Cost");
        costProfile.NullCount.Should().Be(1);
    }

    [Fact]
    public async Task RunFullPipelineEndpoint_EndToEndExecution_ReturnsFourStageSummariesAndMaterializesGold()
    {
        var request = new
        {
            SourceReference = _tempCsvPath,
            DatasetName = "EmployeesCatalog",
            TargetGoldTable = "CuratedStaff"
        };

        var response = await _client.PostAsJsonAsync("/api/data-quality/run-full-pipeline", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PipelineRunResult>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.StageSummaries.Should().HaveCount(5);

        // Verify stage sequence matches Power BI ETL Applied Steps
        var stages = result.StageSummaries.Select(s => s.StageName).ToList();
        stages.Should().ContainInOrder("Profiling", "Deduplication", "Cleaning", "Transformation", "ChartSuggestion");

        // Verify deduplication detected the duplicate row (4 in -> 3 out, 1 quarantined)
        var dedupeSummary = result.StageSummaries.First(s => s.StageName == "Deduplication");
        dedupeSummary.InputRowCount.Should().Be(4);
        dedupeSummary.OutputRowCount.Should().Be(3);
        dedupeSummary.QuarantinedRowCount.Should().Be(1);

        // Verify cleaning stage standardized the 3 distinct rows
        var cleaningSummary = result.StageSummaries.First(s => s.StageName == "Cleaning");
        cleaningSummary.InputRowCount.Should().Be(3);
        cleaningSummary.OutputRowCount.Should().Be(3);

        // Verify transformation stage materialized the Gold table
        var transformSummary = result.StageSummaries.First(s => s.StageName == "Transformation");
        transformSummary.OutputRowCount.Should().Be(3);
        transformSummary.Details.Should().Contain("CuratedStaff");
    }

    [Fact]
    public async Task CleanEndpoint_ExecutesCleaningStageSuccessfully()
    {
        var request = new
        {
            SourceReference = _tempCsvPath,
            DatasetName = "EmployeesCatalog"
        };

        var response = await _client.PostAsJsonAsync("/api/data-quality/clean", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PipelineRunResult>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.StageSummaries.Should().Contain(s => s.StageName == "Cleaning");
    }

    [Fact]
    public async Task TransformEndpoint_ExecutesTransformationPlanStub()
    {
        var request = new
        {
            SilverSourceTable = "SilverStaff",
            TransformationPlanName = "StandardTransform",
            TargetGoldTable = "GoldStaff"
        };

        var response = await _client.PostAsJsonAsync("/api/data-quality/transform", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("GoldStaff");
    }
}
