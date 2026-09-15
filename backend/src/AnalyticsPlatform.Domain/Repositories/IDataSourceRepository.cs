using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Domain.Repositories;

public interface IDataSourceRepository
{
    Task<DataSourceDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DataSourceDefinition dataSource, CancellationToken ct = default);
}

