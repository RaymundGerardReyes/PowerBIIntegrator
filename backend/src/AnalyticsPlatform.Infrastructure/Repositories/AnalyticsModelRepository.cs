using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Infrastructure.Repositories;

public class AnalyticsModelRepository : IAnalyticsModelRepository
{
    private readonly Dictionary<Guid, AnalyticsModel> _store = new();

    public AnalyticsModelRepository()
    {
    }

    public Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var model);
        return Task.FromResult(model);
    }

    public Task<IReadOnlyList<AnalyticsModel>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<AnalyticsModel> list = _store.Values.ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(AnalyticsModel model, CancellationToken ct = default)
    {
        _store[model.Id] = model;
        return Task.CompletedTask;
    }
}
