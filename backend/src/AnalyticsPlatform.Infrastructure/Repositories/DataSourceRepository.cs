using System.Collections.Concurrent;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Infrastructure.Repositories;

public class DataSourceRepository : IDataSourceRepository
{
    private readonly ConcurrentDictionary<Guid, DataSourceDefinition> _store = new();

    public Task<DataSourceDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var dataSource);
        return Task.FromResult(dataSource);
    }

    public Task AddAsync(DataSourceDefinition dataSource, CancellationToken ct = default)
    {
        _store[dataSource.Id] = dataSource;
        return Task.CompletedTask;
    }
}

