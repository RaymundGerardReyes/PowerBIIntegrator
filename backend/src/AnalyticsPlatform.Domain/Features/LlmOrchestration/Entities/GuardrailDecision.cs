using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;

public class GuardrailDecision : Entity
{
    public string CorrelationId { get; private set; }
    public string PolicyId { get; private set; }
    public string UserId { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }
    public LlmProviderType ProviderSelected { get; private set; }
    public IReadOnlyList<GuardrailViolation> Violations { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private GuardrailDecision(
        string correlationId,
        string policyId,
        string userId,
        bool isBlocked,
        string? blockReason,
        LlmProviderType providerSelected,
        IReadOnlyList<GuardrailViolation> violations,
        DateTime createdAtUtc)
    {
        CorrelationId = correlationId;
        PolicyId = policyId;
        UserId = userId;
        IsBlocked = isBlocked;
        BlockReason = blockReason;
        ProviderSelected = providerSelected;
        Violations = violations;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<GuardrailDecision> Allowed(
        string correlationId,
        string policyId,
        string userId,
        LlmProviderType providerSelected,
        IReadOnlyList<GuardrailViolation>? violations = null)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return Result<GuardrailDecision>.Failure("CorrelationId cannot be empty.");

        if (string.IsNullOrWhiteSpace(policyId))
            return Result<GuardrailDecision>.Failure("PolicyId cannot be empty.");

        return Result<GuardrailDecision>.Success(new GuardrailDecision(
            correlationId,
            policyId,
            userId,
            isBlocked: false,
            blockReason: null,
            providerSelected,
            violations ?? Array.Empty<GuardrailViolation>(),
            DateTime.UtcNow));
    }

    public static Result<GuardrailDecision> Blocked(
        string correlationId,
        string policyId,
        string userId,
        string blockReason,
        IReadOnlyList<GuardrailViolation>? violations = null)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return Result<GuardrailDecision>.Failure("CorrelationId cannot be empty.");

        if (string.IsNullOrWhiteSpace(blockReason))
            return Result<GuardrailDecision>.Failure("BlockReason cannot be empty when blocking.");

        return Result<GuardrailDecision>.Success(new GuardrailDecision(
            correlationId,
            policyId,
            userId,
            isBlocked: true,
            blockReason,
            LlmProviderType.LocalOllama,
            violations ?? Array.Empty<GuardrailViolation>(),
            DateTime.UtcNow));
    }
}

