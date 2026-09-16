using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.LlmOrchestration;

public class RunLlmTaskCommandHandlerTests
{
    private readonly ILlmPolicyRepository _policyRepo;
    private readonly ILlmGateway _gateway;
    private readonly RunLlmTaskCommandHandler _handler;

    private readonly LlmPolicy _testPolicy = new(
        PolicyId: "test-policy",
        Name: "Test Policy",
        AllowCloudProvider: true,
        AllowSensitiveContext: false,
        AllowedTools: new[] { "get_analytics_model" },
        MaxTokensPerRequest: 2048,
        MaxDailyTokenBudget: 10000,
        MaximumAllowedSensitivity: SensitivityLevel.Internal
    );

    public RunLlmTaskCommandHandlerTests()
    {
        _policyRepo = Substitute.For<ILlmPolicyRepository>();
        _gateway = Substitute.For<ILlmGateway>();
        _handler = new RunLlmTaskCommandHandler(
            _policyRepo,
            _gateway,
            NullLogger<RunLlmTaskCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenPolicyNotFound_ReturnsFailure()
    {
        _policyRepo.GetPolicyByIdAsync("missing-policy", Arg.Any<CancellationToken>())
            .Returns((LlmPolicy)null!);

        var command = new RunLlmTaskCommand(
            "ExplainDashboard",
            "Explain this visual",
            Array.Empty<string>(),
            LlmProviderType.LocalOllama,
            SensitivityLevel.Public,
            "missing-policy",
            Guid.NewGuid().ToString());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesGatewayAndReturnsSuccess()
    {
        _policyRepo.GetPolicyByIdAsync(_testPolicy.PolicyId, Arg.Any<CancellationToken>())
            .Returns(_testPolicy);

        var expectedResult = LlmTaskResult.Success(
            "Visual explanation text",
            LlmProviderType.CloudOpenAi,
            new TokenUsage(50, 100, 150, 0.002m),
            "test-correlation-id");

        _gateway.InvokeAsync(Arg.Any<LlmTask>(), _testPolicy, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var command = new RunLlmTaskCommand(
            "ExplainDashboard",
            "Explain this visual",
            Array.Empty<string>(),
            LlmProviderType.CloudOpenAi,
            SensitivityLevel.Internal,
            _testPolicy.PolicyId,
            "test-correlation-id");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RawText.Should().Be("Visual explanation text");
        result.Value.ProviderUsed.Should().Be(LlmProviderType.CloudOpenAi);
    }

    [Fact]
    public async Task Handle_WhenSensitivityIsSensitive_ForcesLocalOllamaEvenIfCloudRequested()
    {
        _policyRepo.GetPolicyByIdAsync(_testPolicy.PolicyId, Arg.Any<CancellationToken>())
            .Returns(_testPolicy);

        LlmTask capturedTask = null!;
        _gateway.InvokeAsync(Arg.Do<LlmTask>(t => capturedTask = t), _testPolicy, Arg.Any<CancellationToken>())
            .Returns(callInfo => LlmTaskResult.Success(
                "Local explanation",
                capturedTask.RequestedProvider,
                TokenUsage.Zero,
                capturedTask.CorrelationId));

        var command = new RunLlmTaskCommand(
            "ExplainDashboard",
            "Explain sensitive security event",
            Array.Empty<string>(),
            LlmProviderType.CloudOpenAi, // user requested cloud
            SensitivityLevel.Sensitive,   // but sensitivity is Sensitive!
            _testPolicy.PolicyId,
            "test-correlation-id");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedTask.Should().NotBeNull();
        capturedTask.RequestedProvider.Should().Be(LlmProviderType.LocalOllama);
    }
}

