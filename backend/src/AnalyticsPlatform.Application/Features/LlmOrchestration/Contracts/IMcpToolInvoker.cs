namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public sealed record McpToolInvocationResult(
    bool IsSuccess,
    string? OutputJson,
    string? ErrorMessage
);

public interface IMcpToolInvoker
{
    Task<McpToolInvocationResult> InvokeToolAsync(
        string toolName,
        string argumentsJson,
        string correlationId,
        CancellationToken ct);
}
