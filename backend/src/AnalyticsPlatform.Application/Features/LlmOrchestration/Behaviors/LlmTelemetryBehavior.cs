using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Common;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Behaviors;

public sealed class LlmTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LlmTelemetryBehavior<TRequest, TResponse>> _logger;

    public LlmTelemetryBehavior(ILogger<LlmTelemetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ILlmGuardedRequest guarded)
        {
            return await next();
        }

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[LLM Telemetry Start] Request: {RequestName}, CorrelationId: {CorrelationId}, PolicyId: {PolicyId}",
            typeof(TRequest).Name, guarded.CorrelationId, guarded.PolicyId);

        try
        {
            var response = await next();
            sw.Stop();

            if (response is Result<LlmTaskResult> result && result.IsSuccess && result.Value != null)
            {
                _logger.LogInformation(
                    "[LLM Telemetry Completed] Provider: {Provider}, ElapsedMs: {ElapsedMs}, Tokens: {TotalTokens}, CorrelationId: {CorrelationId}",
                    result.Value.ProviderUsed, sw.ElapsedMilliseconds, result.Value.Usage.TotalTokens, guarded.CorrelationId);
            }
            else
            {
                _logger.LogInformation(
                    "[LLM Telemetry Completed] ElapsedMs: {ElapsedMs}, CorrelationId: {CorrelationId}",
                    sw.ElapsedMilliseconds, guarded.CorrelationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "[LLM Telemetry Error] Request: {RequestName} failed after {ElapsedMs}ms, CorrelationId: {CorrelationId}",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, guarded.CorrelationId);
            throw;
        }
    }
}

