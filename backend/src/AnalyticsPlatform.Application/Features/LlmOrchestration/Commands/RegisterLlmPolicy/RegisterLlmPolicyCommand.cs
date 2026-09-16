using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RegisterLlmPolicy;

public sealed record RegisterLlmPolicyCommand(
    string PolicyId,
    string Name,
    bool AllowCloudProvider,
    bool AllowSensitiveContext,
    IReadOnlyList<string> AllowedTools,
    int MaxTokensPerRequest,
    int MaxDailyTokenBudget,
    SensitivityLevel MaximumAllowedSensitivity
) : IRequest<Result>;

