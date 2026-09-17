using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using Xunit;

namespace AnalyticsPlatform.IntegrationTests.Api;

public class AiAdvisoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AiAdvisoryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAdvisoryPolicies_ReturnsOkWithPredefinedPolicies()
    {
        // Act
        var response = await _client.GetAsync("/api/advisory/policies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Default");
        body.Should().Contain("StrictLocal");
        body.Should().Contain("DataStewardElevated");
    }

    [Fact]
    public async Task PostAdvisoryQuery_ReturnsGroundedAnswerAndCitations()
    {
        // Arrange
        var request = new AdvisoryQueryRequest(
            RunId: "run-demo-001",
            QuestionType: "Duplicates",
            UserQuestion: "Why were rows treated as duplicates?",
            UserRole: "Analyst");

        // Act
        var response = await _client.PostAsJsonAsync("/api/advisory/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AdvisoryResultDto>();
        result.Should().NotBeNull();
        result!.RunId.Should().Be("run-demo-001");
        result.Answer.Should().Contain("ExactHashRule-v2");
        result.CitedRuleIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostAdvisoryUnlock_WithDataSteward_ReturnsOkWithToken()
    {
        // Arrange
        var request = new UnlockConfidentialRequest(
            RunId: "run-demo-001",
            UserId: "steward-01",
            Reason: "Auditing flagged duplicate cluster discrepancies",
            UserRole: "DataSteward");

        // Act
        var response = await _client.PostAsJsonAsync("/api/advisory/unlock", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UnlockConfidentialResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Token.Should().StartWith("unlock-");
    }

    [Fact]
    public async Task PostAdvisoryUnlock_WithUnauthorizedRole_ReturnsBadRequest()
    {
        // Arrange
        var request = new UnlockConfidentialRequest(
            RunId: "run-demo-001",
            UserId: "viewer-01",
            Reason: "Need to view rows",
            UserRole: "Viewer");

        // Act
        var response = await _client.PostAsJsonAsync("/api/advisory/unlock", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

