using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.SecurityAudit.Queries.QueryEventSummary;

public sealed record EventSummaryDto(
    int TotalEventsAnalyzed,
    int BlockedPromptCount,
    int SanitizedPromptCount,
    int PolicyViolationsCount,
    IReadOnlyDictionary<string, int> ProviderExecutionCounts,
    IReadOnlyList<string> RecentSecurityAlerts
);

public sealed record QueryEventSummaryQuery(
    DateTime? SinceUtc = null,
    string? SeverityFilter = null
) : IRequest<Result<EventSummaryDto>>;

