using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly Dictionary<Guid, DashboardDefinition> _store = new();

    public Task<DashboardDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var dashboard);
        return Task.FromResult(dashboard);
    }

    public Task AddAsync(DashboardDefinition dashboard, CancellationToken ct = default)
    {
        _store[dashboard.Id] = dashboard;
        return Task.CompletedTask;
    }
}
