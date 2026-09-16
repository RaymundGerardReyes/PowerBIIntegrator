using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Llm.Policy;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.Llm;

public class ProviderRouterTests
{
    private readonly IOllamaClient _ollamaClient;
    private readonly ProviderRouter _router;

    public ProviderRouterTests()
    {
        _ollamaClient = Substitute.For<IOllamaClient>();
        _router = new ProviderRouter(_ollamaClient, NullLogger<ProviderRouter>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenLocalOllamaRequested_InvokesOllamaClientDirectly()
    {
        _ollamaClient.ChatAsync("llama3.3:8b-instruct", "Analyze measures", ct: Arg.Any<CancellationToken>())
            .Returns("Analysis of revenue measures.");

        var task = LlmTask.Create(
            "ExplainMeasures",
            "Analyze measures",
            Array.Empty<string>(),
            LlmProviderType.LocalOllama,
            SensitivityLevel.Internal,
            "corr-1").Value!;

        var policy = new LlmPolicy("default", "Default", false, false, Array.Empty<string>(), 2048, 10000, SensitivityLevel.Internal);

        var result = await _router.InvokeAsync(task, policy, CancellationToken.None);

        result.IsBlocked.Should().BeFalse();
        result.RawText.Should().Be("Analysis of revenue measures.");
        result.ProviderUsed.Should().Be(LlmProviderType.LocalOllama);
        await _ollamaClient.Received(1).ChatAsync("llama3.3:8b-instruct", "Analyze measures", ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenCloudRequested_FallsBackToLocalOllamaWithNotice()
    {
        _ollamaClient.ChatAsync("llama3.3:8b-instruct", "Analyze measures", ct: Arg.Any<CancellationToken>())
            .Returns("Local fallback analysis.");

        var task = LlmTask.Create(
            "ExplainMeasures",
            "Analyze measures",
            Array.Empty<string>(),
            LlmProviderType.CloudOpenAi,
            SensitivityLevel.Internal,
            "corr-2").Value!;

        var policy = new LlmPolicy("analytics-nonsensitive", "Analytics", true, false, Array.Empty<string>(), 4096, 50000, SensitivityLevel.Internal);

        var result = await _router.InvokeAsync(task, policy, CancellationToken.None);

        result.IsBlocked.Should().BeFalse();
        result.RawText.Should().Be("Local fallback analysis.");
        result.GuardrailNotice.Should().Contain("routed to Local Ollama");
    }
}
