using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;

public sealed record LlmTaskResult(
    string RawText,
    LlmProviderType ProviderUsed,
    bool IsBlocked,
    string? BlockReason,
    string? GuardrailNotice,
    TokenUsage Usage,
    string CorrelationId
)
{
    public static LlmTaskResult Success(
        string text,
        LlmProviderType provider,
        TokenUsage usage,
        string correlationId,
        string? guardrailNotice = null)
        => new(text, provider, false, null, guardrailNotice, usage, correlationId);

    public static LlmTaskResult Blocked(
        string reason,
        string correlationId,
        string? guardrailNotice = null)
        => new(string.Empty, LlmProviderType.LocalOllama, true, reason, guardrailNotice, TokenUsage.Zero, correlationId);
}

