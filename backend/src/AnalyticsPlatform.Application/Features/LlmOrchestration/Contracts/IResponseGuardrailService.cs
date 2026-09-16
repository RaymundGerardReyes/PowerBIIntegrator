using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public sealed record ResponseFilterResult(
    bool IsBlocked,
    string? BlockReason,
    string FilteredText,
    string? GuardrailNotice
);

public interface IResponseGuardrailService
{
    Task<ResponseFilterResult> FilterAsync(
        string rawText,
        LlmPolicy policy,
        string correlationId,
        CancellationToken ct);
}
