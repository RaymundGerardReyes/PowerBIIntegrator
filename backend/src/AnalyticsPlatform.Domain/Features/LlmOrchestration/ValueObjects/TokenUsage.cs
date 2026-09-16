namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

public sealed record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd
)
{
    public static TokenUsage Zero => new(0, 0, 0, 0m);
}
