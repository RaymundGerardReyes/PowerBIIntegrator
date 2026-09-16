using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Common;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Behaviors;

public sealed class ResponseGuardrailBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IResponseGuardrailService _guardrails;
    private readonly ILlmPolicyRepository _policies;
    private readonly ILogger<ResponseGuardrailBehavior<TRequest, TResponse>> _logger;

    public ResponseGuardrailBehavior(
        IResponseGuardrailService guardrails,
        ILlmPolicyRepository policies,
        ILogger<ResponseGuardrailBehavior<TRequest, TResponse>> logger)
    {
        _guardrails = guardrails;
        _policies = policies;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ILlmGuardedRequest guarded && response is Result<LlmTaskResult> result && result.IsSuccess && result.Value != null)
        {
            var policy = await _policies.GetPolicyByIdAsync(guarded.PolicyId, cancellationToken);
            var filtered = await _guardrails.FilterAsync(
                result.Value.RawText,
                policy,
                guarded.CorrelationId,
                cancellationToken);

            if (filtered.IsBlocked)
            {
                _logger.LogWarning(
                    "[Response Filter Blocked] LLM response blocked by reason: {Reason} (CorrelationId: {CorrelationId})",
                    filtered.BlockReason, guarded.CorrelationId);

                return (TResponse)(object)Result<LlmTaskResult>.Failure($"Response blocked: {filtered.BlockReason}");
            }

            if (filtered.FilteredText != result.Value.RawText || filtered.GuardrailNotice != null)
            {
                var modified = result.Value with
                {
                    RawText = filtered.FilteredText,
                    GuardrailNotice = filtered.GuardrailNotice
                };

                return (TResponse)(object)Result<LlmTaskResult>.Success(modified);
            }
        }

        return response;
    }
}

