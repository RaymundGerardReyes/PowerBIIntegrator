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

    public Task<IReadOnlyList<DataSourceDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<DataSourceDefinition> list = _store.Values.ToList();
        return Task.FromResult(list);
    }

    public Task<DataSourceDefinition?> GetByNameOrPathAsync(string nameOrPath, CancellationToken ct = default)
    {
        var match = _store.Values.FirstOrDefault(ds =>
            string.Equals(ds.Name, nameOrPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ds.ConnectionOrPath, nameOrPath, StringComparison.OrdinalIgnoreCase) ||
            ds.Id.ToString().Equals(nameOrPath, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task AddAsync(DataSourceDefinition dataSource, CancellationToken ct = default)
    {
        _store[dataSource.Id] = dataSource;
        return Task.CompletedTask;
    }
}

