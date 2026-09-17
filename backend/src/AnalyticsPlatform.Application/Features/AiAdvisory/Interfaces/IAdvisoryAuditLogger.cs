using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

public interface IAdvisoryAuditLogger
{
    Task LogAdvisoryQueryAsync(
        AdvisoryResult result,
        string policyApplied,
        string chosenProvider,
        int totalFieldsInspected,
        int redactedCount,
        CancellationToken ct = default);

    Task LogConfidentialUnlockAsync(
        string runId,
        string userId,
        string reason,
        CancellationToken ct = default);
}

