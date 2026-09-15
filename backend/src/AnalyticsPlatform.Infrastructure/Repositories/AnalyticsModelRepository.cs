using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Infrastructure.Repositories;

public class AnalyticsModelRepository : IAnalyticsModelRepository
{
    private readonly Dictionary<Guid, AnalyticsModel> _store = new();

    public Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task AddAsync(AnalyticsModel model, CancellationToken ct = default)
    {
        _store[model.Id] = model;
        return Task.CompletedTask;
    }
}
