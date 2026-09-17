using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Common;

public sealed record AdvisoryQueryRequest(
    string RunId,
    string QuestionType,
    string UserQuestion,
    string? UserId = null,
    string? UserRole = null,
    bool UnlockConfidential = false,
    string? CorrelationId = null);

public sealed record AdvisoryResultDto(
    Guid Id,
    string RunId,
    string QuestionType,
    string UserQuestion,
    string Answer,
    string ProviderUsed,
    IReadOnlyList<string> CitedRuleIds,
    IReadOnlyList<string> CitedRunIds,
    int RedactedFieldsCount,
    bool IsUnlockedConfidential,
    string CorrelationId,
    DateTime TimestampUtc);

public sealed record AdvisoryPolicyDto(
    string PolicyName,
    bool AllowCloudProvider,
    string MaxExposureLevel,
    IReadOnlyList<string> AllowedTools,
    bool RequireHumanApprovalForConfidential);

public sealed record UnlockConfidentialRequest(
    string RunId,
    string UserId,
    string Reason,
    string UserRole);

public sealed record UnlockConfidentialResultDto(
    bool Success,
    string Token,
    string Message,
    DateTime ExpiresAtUtc);

public sealed record SchemaViolationSummary(
    string RuleId,
    string RuleName,
    string ColumnName,
    string ExpectedType,
    string ActualType,
    string Severity);

public sealed record ChartSuggestionSummary(
    string RuleId,
    string ChartType,
    string SuggestedMeasure,
    string CategoryColumn,
    string Reason);

