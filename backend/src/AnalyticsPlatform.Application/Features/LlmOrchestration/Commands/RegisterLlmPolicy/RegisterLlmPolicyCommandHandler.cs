using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RegisterLlmPolicy;

public sealed class RegisterLlmPolicyCommandHandler : IRequestHandler<RegisterLlmPolicyCommand, Result>
{
    private readonly ILlmPolicyRepository _policies;

    public RegisterLlmPolicyCommandHandler(ILlmPolicyRepository policies)
    {
        _policies = policies;
    }

    public async Task<Result> Handle(RegisterLlmPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = new LlmPolicy(
            request.PolicyId,
            request.Name,
            request.AllowCloudProvider,
            request.AllowSensitiveContext,
            request.AllowedTools,
            request.MaxTokensPerRequest,
            request.MaxDailyTokenBudget,
            request.MaximumAllowedSensitivity
        );

        await _policies.RegisterPolicyAsync(policy, cancellationToken);
        return Result.Success();
    }
}

