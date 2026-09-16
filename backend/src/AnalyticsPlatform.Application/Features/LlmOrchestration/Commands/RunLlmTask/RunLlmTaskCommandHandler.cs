using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;

public sealed class RunLlmTaskCommandHandler : IRequestHandler<RunLlmTaskCommand, Result<LlmTaskResult>>
{
    private readonly ILlmPolicyRepository _policies;
    private readonly ILlmGateway _gateway;
    private readonly ILogger<RunLlmTaskCommandHandler> _logger;

    public RunLlmTaskCommandHandler(
        ILlmPolicyRepository policies,
        ILlmGateway gateway,
        ILogger<RunLlmTaskCommandHandler> logger)
    {
        _policies = policies;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<LlmTaskResult>> Handle(RunLlmTaskCommand request, CancellationToken cancellationToken)
    {
        var policy = await _policies.GetPolicyByIdAsync(request.PolicyId, cancellationToken);
        if (policy == null)
        {
            return Result<LlmTaskResult>.Failure($"Policy '{request.PolicyId}' not found.");
        }

        var providerResolution = ProviderSelectionRules.ResolveProvider(
            policy,
            request.Sensitivity,
            request.ProviderPreference,
            userHasCloudPrivilege: true);

        if (!providerResolution.IsSuccess)
        {
            return Result<LlmTaskResult>.Failure(providerResolution.Errors);
        }

        var effectiveProvider = providerResolution.Value;

        var taskCreation = LlmTask.Create(
            request.TaskType,
            request.SanitizedPrompt,
            request.ContextIds,
            effectiveProvider,
            request.Sensitivity,
            request.CorrelationId);

        if (!taskCreation.IsSuccess || taskCreation.Value == null)
        {
            return Result<LlmTaskResult>.Failure(taskCreation.Errors);
        }

        var task = taskCreation.Value;
        var result = await _gateway.InvokeAsync(task, policy, cancellationToken);

        return Result<LlmTaskResult>.Success(result);
    }
}

