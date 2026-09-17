namespace AnalyticsPlatform.Domain.Features.AiAdvisory.ValueObjects;

public enum ExposureDecisionType
{
    IncludeAsIs = 0,
    Redacted = 1,
    HeldPendingApproval = 2,
    BlockedRestricted = 3
}

public sealed record ExposureDecision(
    ExposureDecisionType Decision,
    string? MaskedPlaceholder = null,
    string? Reason = null)
{
    public static ExposureDecision Include() => new(ExposureDecisionType.IncludeAsIs);
    public static ExposureDecision Redact(string fieldType) => new(
        ExposureDecisionType.Redacted,
        $"[REDACTED: {fieldType}]",
        $"Field sensitivity exceeds current advisory policy threshold.");
    public static ExposureDecision HoldPendingApproval(string fieldType) => new(
        ExposureDecisionType.HeldPendingApproval,
        $"[PENDING_APPROVAL: {fieldType}]",
        "Sensitive data requires explicit human DataSteward unlock.");
    public static ExposureDecision BlockRestricted() => new(
        ExposureDecisionType.BlockedRestricted,
        "[BLOCKED: RESTRICTED]",
        "Restricted data is strictly forbidden from entering any LLM context under all conditions.");
}

