namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

public sealed record GuardrailViolation(
    string Code,
    string Description,
    SensitivityLevel Severity
);
