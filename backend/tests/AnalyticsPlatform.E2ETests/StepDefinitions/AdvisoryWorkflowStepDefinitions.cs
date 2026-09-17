using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using Reqnroll;

namespace AnalyticsPlatform.E2ETests.StepDefinitions;

[Binding]
public class AdvisoryWorkflowStepDefinitions : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private string _runId = string.Empty;
    private HttpResponseMessage? _unlockResponse;
    private UnlockConfidentialResultDto? _unlockResult;
    private AdvisoryResultDto? _advisoryResult;
    private bool _disposed;

    public AdvisoryWorkflowStepDefinitions()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Given("a completed pipeline run with confidential sample rows")]
    public void GivenACompletedPipelineRunWithConfidentialSampleRows()
    {
        _runId = "run-demo-001";
    }

    [When("a DataSteward requests to unlock confidential exposure with a valid audit reason")]
    public async Task WhenADataStewardRequestsToUnlockConfidentialExposureWithAValidAuditReason()
    {
        var request = new UnlockConfidentialRequest(
            RunId: _runId,
            UserId: "steward-01",
            Reason: "Auditing flagged duplicate cluster discrepancies",
            UserRole: "DataSteward");

        _unlockResponse = await _client.PostAsJsonAsync("/api/advisory/unlock", request);
        _unlockResponse.EnsureSuccessStatusCode();
        _unlockResult = await _unlockResponse.Content.ReadFromJsonAsync<UnlockConfidentialResultDto>();
    }

    [Then("the unlock request should succeed with an audit token")]
    public void ThenTheUnlockRequestShouldSucceedWithAnAuditToken()
    {
        _unlockResponse.Should().NotBeNull();
        _unlockResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        _unlockResult.Should().NotBeNull();
        _unlockResult!.Success.Should().BeTrue();
        _unlockResult.Token.Should().StartWith("unlock-");
    }

    [Then("subsequent advisory query for that run should execute locally via LocalOllama")]
    public async Task ThenSubsequentAdvisoryQueryForThatRunShouldExecuteLocallyViaLocalOllama()
    {
        var queryRequest = new AdvisoryQueryRequest(
            RunId: _runId,
            QuestionType: "Duplicates",
            UserQuestion: "Explain duplicate clusters with confidential sample values",
            UserRole: "DataSteward",
            UnlockConfidential: true);

        var queryResponse = await _client.PostAsJsonAsync("/api/advisory/query", queryRequest);
        queryResponse.EnsureSuccessStatusCode();
        _advisoryResult = await queryResponse.Content.ReadFromJsonAsync<AdvisoryResultDto>();

        _advisoryResult.Should().NotBeNull();
        _advisoryResult!.ProviderUsed.Should().Be("LocalOllama");
    }

    [Then("the advisory response should contain verified rule citations")]
    public void ThenTheAdvisoryResponseShouldContainVerifiedRuleCitations()
    {
        _advisoryResult.Should().NotBeNull();
        _advisoryResult!.CitedRuleIds.Should().NotBeEmpty();
        _advisoryResult.CitedRuleIds.Should().Contain("ExactHashRule-v2");
        _advisoryResult.CitedRunIds.Should().Contain(_runId);
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

