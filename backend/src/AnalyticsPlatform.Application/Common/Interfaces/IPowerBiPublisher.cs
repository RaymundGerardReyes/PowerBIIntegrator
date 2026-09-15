using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IPowerBiPublisher
{
    Task<string> PublishAsync(DashboardDefinition dashboard, string workspaceId, CancellationToken ct = default);
}
