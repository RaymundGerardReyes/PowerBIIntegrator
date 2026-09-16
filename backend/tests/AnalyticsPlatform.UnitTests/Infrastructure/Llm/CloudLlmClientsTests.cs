using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Llm.Policy;
using AnalyticsPlatform.Infrastructure.Llm.Providers;
using AnalyticsPlatform.Infrastructure.Llm.Resilience;
using NSubstitute;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.Llm;

public sealed class CloudLlmClientsTests
{
    [Fact]
    public async Task OpenAiCloudClient_WhenApiKeyMissing_IsConfiguredAsyncReturnsFalse()
    {
        var config = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        var client = new OpenAiCloudClient(httpClient, config, NullLogger<OpenAiCloudClient>.Instance);

        var configured = await client.IsConfiguredAsync();
        configured.Should().BeFalse();
        client.ProviderType.Should().Be(LlmProviderType.CloudOpenAi);
    }

    [Fact]
    public async Task AnthropicCloudClient_WhenApiKeyMissing_IsConfiguredAsyncReturnsFalse()
    {
        var config = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        var client = new AnthropicCloudClient(httpClient, config, NullLogger<AnthropicCloudClient>.Instance);

        var configured = await client.IsConfiguredAsync();
        configured.Should().BeFalse();
        client.ProviderType.Should().Be(LlmProviderType.CloudAnthropic);
    }

    [Fact]
    public void PollyLlmResilience_CreateLlmPipeline_ReturnsValidPipeline()
    {
        var pipeline = PollyLlmResilience.CreateLlmPipeline();
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ProviderRouter_WhenCloudClientConfiguredAndPolicyAllows_RoutesToCloudClient()
    {
        var ollama = Substitute.For<IOllamaClient>();
        var openAiClient = Substitute.For<ICloudLlmClient>();
        openAiClient.ProviderType.Returns(LlmProviderType.CloudOpenAi);
        openAiClient.IsConfiguredAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        openAiClient.ChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("OpenAI Cloud Response"));

        var router = new ProviderRouter(ollama, new[] { openAiClient }, NullLogger<ProviderRouter>.Instance);

        var task = LlmTask.Create("ExplainDashboard", "Analyze revenue", Array.Empty<string>(), LlmProviderType.CloudOpenAi, SensitivityLevel.Public, "corr-100").Value!;
        var policy = new LlmPolicy("policy-cloud", "Cloud Policy", AllowCloudProvider: true, AllowSensitiveContext: true, Array.Empty<string>(), 2000, 10000, SensitivityLevel.Sensitive);

        var result = await router.InvokeAsync(task, policy, CancellationToken.None);

        result.IsBlocked.Should().BeFalse();
        result.RawText.Should().Be("OpenAI Cloud Response");
    }
}
