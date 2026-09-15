using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IDataSourceSchemaExtractor
{
    Task<IReadOnlyList<ColumnSchema>> ExtractSchemaAsync(string connectionOrPath, CancellationToken ct = default);
}

