using FluentAssertions;
using AnalyticsPlatform.Application.Features.SecurityAudit.Queries.QueryEventSummary;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.SecurityAudit;

public sealed class QueryEventSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAggregateSecurityMetrics()
    {
        var handler = new QueryEventSummaryQueryHandler();
        var query = new QueryEventSummaryQuery(DateTime.UtcNow.AddDays(-7));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalEventsAnalyzed.Should().BeGreaterThan(0);
        result.Value.ProviderExecutionCounts.Should().ContainKey("LocalOllama");
        result.Value.RecentSecurityAlerts.Should().NotBeEmpty();
    }
}

