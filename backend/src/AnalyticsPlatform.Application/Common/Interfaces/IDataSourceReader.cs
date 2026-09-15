namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IDataSourceReader
{
    Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default);
    IAsyncEnumerable<IReadOnlyList<IDictionary<string, object?>>> ReadBatchesAsync(string connectionOrPath, int batchSize = 1000, CancellationToken ct = default);
}
