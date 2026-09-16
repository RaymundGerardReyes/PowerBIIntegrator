using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public sealed record GuardrailScanResult(
    bool IsBlocked,
    string? BlockReason,
    string SanitizedPrompt,
    IReadOnlyList<GuardrailViolation> Violations
);

public interface IPromptGuardrailService
{
    Task<GuardrailScanResult> ValidateAndSanitizeAsync(
        string prompt,
        IReadOnlyList<string> contextIds,
        LlmPolicy policy,
        string correlationId,
        CancellationToken ct);
}
