using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Infrastructure.Llm.Policy;

public sealed class ProviderRouter : ILlmGateway
{
    private readonly IOllamaClient _ollamaClient;
    private readonly ILogger<ProviderRouter> _logger;

    public ProviderRouter(IOllamaClient ollamaClient, ILogger<ProviderRouter> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public async Task<LlmTaskResult> InvokeAsync(LlmTask task, LlmPolicy policy, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(policy);

        _logger.LogInformation(
            "[ProviderRouter] Dispatching task {TaskType} to provider {Provider} (CorrelationId: {CorrelationId})",
            task.TaskType, task.RequestedProvider, task.CorrelationId);

        if (task.RequestedProvider == LlmProviderType.LocalOllama)
        {
            return await ExecuteLocalOllamaAsync(task, ct);
        }

        // Cloud provider requested - verify policy permits
        if (policy.AllowCloudProvider)
        {
            // When cloud credentials or remote client are not configured in environment,
            // we safely fall back to LocalOllama and report the downgrade notice
            _logger.LogWarning(
                "[ProviderRouter] Cloud provider {Provider} requested, falling back to LocalOllama (CorrelationId: {CorrelationId})",
                task.RequestedProvider, task.CorrelationId);

            var fallbackResult = await ExecuteLocalOllamaAsync(task, ct);
            return fallbackResult with
            {
                GuardrailNotice = $"Cloud provider ({task.RequestedProvider}) routed to Local Ollama per environment policy."
            };
        }

        // Policy strictly forbids cloud
        _logger.LogWarning(
            "[ProviderRouter] Cloud provider forbidden by policy '{PolicyId}', forced to LocalOllama (CorrelationId: {CorrelationId})",
            policy.PolicyId, task.CorrelationId);

        var localResult = await ExecuteLocalOllamaAsync(task, ct);
        return localResult with
        {
            GuardrailNotice = "Policy forbids cloud transmission; request was executed securely on local Ollama runtime."
        };
    }

    private async Task<LlmTaskResult> ExecuteLocalOllamaAsync(LlmTask task, CancellationToken ct)
    {
        try
        {
            var text = await _ollamaClient.ChatAsync("llama3.3:8b-instruct", task.SanitizedPrompt, ct: ct);
            var estimatedTokens = (task.SanitizedPrompt.Length / 4) + (text.Length / 4);

            return LlmTaskResult.Success(
                text,
                LlmProviderType.LocalOllama,
                new TokenUsage(task.SanitizedPrompt.Length / 4, text.Length / 4, estimatedTokens, 0m),
                task.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProviderRouter] Error executing Local Ollama task (CorrelationId: {CorrelationId})", task.CorrelationId);
            return LlmTaskResult.Blocked($"Local LLM execution failure: {ex.Message}", task.CorrelationId);
        }
    }
}
