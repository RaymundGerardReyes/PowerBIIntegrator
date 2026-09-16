using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Common;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Behaviors;

public sealed class PromptGuardrailBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPromptGuardrailService _guardrails;
    private readonly ILlmPolicyRepository _policies;
    private readonly ILogger<PromptGuardrailBehavior<TRequest, TResponse>> _logger;

    public PromptGuardrailBehavior(
        IPromptGuardrailService guardrails,
        ILlmPolicyRepository policies,
        ILogger<PromptGuardrailBehavior<TRequest, TResponse>> logger)
    {
        _guardrails = guardrails;
        _policies = policies;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ILlmGuardedRequest guarded)
        {
            var policy = await _policies.GetPolicyByIdAsync(guarded.PolicyId, cancellationToken);
            var scan = await _guardrails.ValidateAndSanitizeAsync(
                guarded.UserPrompt,
                guarded.ContextIds,
                policy,
                guarded.CorrelationId,
                cancellationToken);

            if (scan.IsBlocked)
            {
                _logger.LogWarning(
                    "[Guardrail Blocked] Task blocked by reason: {Reason} (CorrelationId: {CorrelationId})",
                    scan.BlockReason, guarded.CorrelationId);

                if (typeof(Result).IsAssignableFrom(typeof(TResponse)))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", new[] { typeof(string[]) });
                    if (failureMethod != null)
                    {
                        var resultObj = failureMethod.Invoke(null, new object[] { new[] { $"Guardrail blocked: {scan.BlockReason}" } });
                        if (resultObj is TResponse typedResult)
                        {
                            return typedResult;
                        }
                    }
                }

                throw new InvalidOperationException($"Guardrail blocked: {scan.BlockReason}");
            }

            guarded.UpdateSanitizedPrompt(scan.SanitizedPrompt);
        }

        return await next();
    }
}
