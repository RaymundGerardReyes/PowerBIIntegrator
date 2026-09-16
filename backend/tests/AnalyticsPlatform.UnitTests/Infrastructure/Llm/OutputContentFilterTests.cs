using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Llm.Guardrails;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.Llm;

public class OutputContentFilterTests
{
    private readonly OutputContentFilter _filter;
    private readonly LlmPolicy _policy = new(
        PolicyId: "default",
        Name: "Default",
        AllowCloudProvider: false,
        AllowSensitiveContext: false,
        AllowedTools: Array.Empty<string>(),
        MaxTokensPerRequest: 2048,
        MaxDailyTokenBudget: 10000,
        MaximumAllowedSensitivity: SensitivityLevel.Internal
    );

    public OutputContentFilterTests()
    {
        _filter = new OutputContentFilter(NullLogger<OutputContentFilter>.Instance);
    }

    [Fact]
    public async Task FilterAsync_WhenResponseContainsAwsSecretKey_BlocksResponse()
    {
        var rawText = "Here is the key: AKIAIOSFODNN7EXAMPLE for deployment.";
        var result = await _filter.FilterAsync(rawText, _policy, "corr-1", CancellationToken.None);

        result.IsBlocked.Should().BeTrue();
        result.BlockReason.Should().Contain("credential or API key leakage");
    }

    [Fact]
    public async Task FilterAsync_WhenResponseContainsOpenAiApiKey_BlocksResponse()
    {
        var rawText = "Use sk-abcdefghijklmnopqrstuvwxyz1234567890 to authenticate.";
        var result = await _filter.FilterAsync(rawText, _policy, "corr-2", CancellationToken.None);

        result.IsBlocked.Should().BeTrue();
        result.BlockReason.Should().Contain("credential or API key leakage");
    }

    [Fact]
    public async Task FilterAsync_WhenCleanText_PassesThrough()
    {
        var rawText = "Total revenue increased by 14.2% quarter-over-quarter.";
        var result = await _filter.FilterAsync(rawText, _policy, "corr-3", CancellationToken.None);

        result.IsBlocked.Should().BeFalse();
        result.FilteredText.Should().Be(rawText);
        result.GuardrailNotice.Should().BeNull();
    }
}

