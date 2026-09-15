using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Domain.Repositories;

public interface IDashboardRepository
{
    Task<DashboardDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DashboardDefinition dashboard, CancellationToken ct = default);
}
