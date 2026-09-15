using System.Runtime.CompilerServices;
using ClosedXML.Excel;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;

public class ExcelDataSourceReader : IDataSourceReader, IDataSourceSchemaExtractor
{
    private const int SchemaInferenceSampleRows = 200;

    public Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(new List<IDictionary<string, object?>>());

        using var workbook = new XLWorkbook(connectionOrPath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(new List<IDictionary<string, object?>>());

        var headers = GetHeaders(worksheet);
        var rows = new List<IDictionary<string, object?>>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                record[headers[i]] = ExtractCellValue(row.Cell(i + 1));
            }
            rows.Add(record);
        }

        return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows);
    }

    public async IAsyncEnumerable<IReadOnlyList<IDictionary<string, object?>>> ReadBatchesAsync(
        string connectionOrPath,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            yield break;

        using var workbook = new XLWorkbook(connectionOrPath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            yield break;

        var headers = GetHeaders(worksheet);
        var currentBatch = new List<IDictionary<string, object?>>(batchSize);

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                record[headers[i]] = ExtractCellValue(row.Cell(i + 1));
            }
            currentBatch.Add(record);

            if (currentBatch.Count >= batchSize)
            {
                yield return currentBatch.ToList();
                currentBatch.Clear();
            }
        }

        if (currentBatch.Count > 0)
        {
            yield return currentBatch;
        }

        await Task.CompletedTask;
    }

    public Task<IReadOnlyList<ColumnSchema>> ExtractSchemaAsync(string connectionOrPath, CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            return Task.FromResult<IReadOnlyList<ColumnSchema>>(new List<ColumnSchema>());

        using var workbook = new XLWorkbook(connectionOrPath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return Task.FromResult<IReadOnlyList<ColumnSchema>>(new List<ColumnSchema>());

        var headers = GetHeaders(worksheet);
        var sampleRows = worksheet.RowsUsed().Skip(1).Take(SchemaInferenceSampleRows).ToList();

        var schemaList = new List<ColumnSchema>(headers.Count);

        for (int i = 0; i < headers.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var colIndex = i + 1;
            var values = sampleRows.Select(r => ExtractCellValue(r.Cell(colIndex))).ToList();
            var inference = TypeInferenceEngine.InferColumn(i, headers[i], values);
            schemaList.Add(inference.Schema);
        }

        return Task.FromResult<IReadOnlyList<ColumnSchema>>(schemaList);
    }

    private static void ValidatePath(string connectionOrPath)
    {
        if (string.IsNullOrWhiteSpace(connectionOrPath))
            throw new ArgumentException("Path cannot be empty.", nameof(connectionOrPath));

        if (connectionOrPath.Contains(".."))
            throw new InvalidOperationException($"Potential directory traversal detected in path: '{connectionOrPath}'.");
    }

    private static List<string> GetHeaders(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        return headerRow.CellsUsed().Select(c => c.GetString().Trim()).Where(h => !string.IsNullOrEmpty(h)).ToList();
    }

    private static object? ExtractCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        var val = cell.Value;
        return val.Type switch
        {
            XLDataType.Boolean => val.GetBoolean(),
            XLDataType.Number => val.GetNumber(),
            XLDataType.DateTime => val.GetDateTime(),
            XLDataType.Text => val.GetText(),
            _ => val.ToString()
        };
    }
}
