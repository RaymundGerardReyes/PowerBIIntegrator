using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Repositories;

public class SchemaContractRepository
{
    private readonly Dictionary<string, SchemaContract> _contracts = new();

    public Task SaveContractAsync(SchemaContract contract, CancellationToken ct = default)
    {
        _contracts[contract.TableName.ToLowerInvariant()] = contract;
        return Task.CompletedTask;
    }

    public Task<SchemaContract?> GetContractAsync(string tableName, CancellationToken ct = default)
    {
        _contracts.TryGetValue(tableName.ToLowerInvariant(), out var contract);
        return Task.FromResult(contract);
    }
}
