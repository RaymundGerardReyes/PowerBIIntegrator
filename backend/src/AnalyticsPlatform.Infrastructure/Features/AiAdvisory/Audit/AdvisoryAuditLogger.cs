using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Audit;

public sealed class AdvisoryAuditLogger : IAdvisoryAuditLogger
{
    private readonly ILogger<AdvisoryAuditLogger> _logger;

    public AdvisoryAuditLogger(ILogger<AdvisoryAuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task LogAdvisoryQueryAsync(
        AdvisoryResult result,
        string policyApplied,
        string chosenProvider,
        int totalFieldsInspected,
        int redactedCount,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[AdvisoryAudit] QueryExecuted | RunId: {RunId} | QuestionType: {QuestionType} | Provider: {Provider} | Policy: {Policy} | RedactedCount: {Redacted} | GroundedRules: {GroundedRules} | CorrelationId: {CorrelationId}",
            result.RunId,
            result.QuestionType,
            chosenProvider,
            policyApplied,
            redactedCount,
            string.Join(", ", result.CitedRuleIds),
            result.CorrelationId);

        return Task.CompletedTask;
    }

    public Task LogConfidentialUnlockAsync(
        string runId,
        string userId,
        string reason,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[AdvisoryAudit] ConfidentialUnlockRequested | RunId: {RunId} | UserId: {UserId} | Reason: {Reason} | Timestamp: {Timestamp}",
            runId,
            userId,
            reason,
            DateTime.UtcNow);

        return Task.CompletedTask;
    }
}

