using AnalyticsPlatform.Infrastructure.Features.DataQuality.Connectors;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Persistence;

public record QuarantinedRow(
    string BatchId,
    string RowId,
    string ReasonCode,
    string RowPayloadJson,
    DateTime QuarantinedAtUtc
);

public class QuarantineWriter
{
    private readonly List<QuarantinedRow> _quarantineStore = new();

    public Task WriteQuarantineAsync(string batchId, IEnumerable<TabularRow> rejectedRows, string reasonCode, CancellationToken ct = default)
    {
        foreach (var row in rejectedRows)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(row.Fields);
            _quarantineStore.Add(new QuarantinedRow(batchId, row.RowId, reasonCode, payload, DateTime.UtcNow));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<QuarantinedRow>> GetQuarantinedRowsAsync(string batchId, CancellationToken ct = default)
    {
        var result = _quarantineStore.Where(q => q.BatchId.Equals(batchId, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<QuarantinedRow>>(result);
    }
}
