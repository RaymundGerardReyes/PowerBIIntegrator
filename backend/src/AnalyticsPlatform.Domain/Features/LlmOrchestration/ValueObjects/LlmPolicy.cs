namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

public sealed record LlmPolicy(
    string PolicyId,
    string Name,
    bool AllowCloudProvider,
    bool AllowSensitiveContext,
    IReadOnlyList<string> AllowedTools,
    int MaxTokensPerRequest,
    int MaxDailyTokenBudget,
    SensitivityLevel MaximumAllowedSensitivity
);

