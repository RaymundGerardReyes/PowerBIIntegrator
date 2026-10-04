namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public record TabularRow(string RowId, Dictionary<string, string?> Fields);

public record TabularBatch(string SourceName, IReadOnlyList<string> Columns, IReadOnlyList<TabularRow> Rows);
