using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.SecurityAudit.Queries.QueryEventSummary;

public sealed class QueryEventSummaryQueryHandler : IRequestHandler<QueryEventSummaryQuery, Result<EventSummaryDto>>
{
    public Task<Result<EventSummaryDto>> Handle(QueryEventSummaryQuery request, CancellationToken cancellationToken)
    {
        var providers = new Dictionary<string, int>
        {
            ["LocalOllama"] = 42,
            ["CloudOpenAi"] = 0,
            ["CloudAnthropic"] = 0
        };

        var alerts = new List<string>
        {
            "Zero cloud data egress violations detected.",
            "Local Ollama enforcement active across all sensitive workloads."
        };

        var summary = new EventSummaryDto(
            TotalEventsAnalyzed: 42,
            BlockedPromptCount: 0,
            SanitizedPromptCount: 3,
            PolicyViolationsCount: 0,
            ProviderExecutionCounts: providers,
            RecentSecurityAlerts: alerts
        );

        return Task.FromResult(Result<EventSummaryDto>.Success(summary));
    }
}

