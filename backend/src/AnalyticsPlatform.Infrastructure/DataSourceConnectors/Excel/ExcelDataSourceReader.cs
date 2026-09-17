using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ExcelDataReader;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;

public class ExcelDataSourceReader : IDataSourceReader, IDataSourceSchemaExtractor
{
    private const int SchemaInferenceSampleRows = 200;

    static ExcelDataSourceReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(new List<IDictionary<string, object?>>());

        using var stream = File.Open(connectionOrPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var rows = new List<IDictionary<string, object?>>();
        if (!reader.Read())
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows);

        var headers = GetHeaders(reader);
        if (headers.Count == 0)
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(rows);

        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var val = i < reader.FieldCount ? reader.GetValue(i) : null;
                record[headers[i]] = NormalizeCellValue(val);
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

        using var stream = File.Open(connectionOrPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (!reader.Read())
            yield break;

        var headers = GetHeaders(reader);
        if (headers.Count == 0)
            yield break;

        var currentBatch = new List<IDictionary<string, object?>>(batchSize);

        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var val = i < reader.FieldCount ? reader.GetValue(i) : null;
                record[headers[i]] = NormalizeCellValue(val);
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

        using var stream = File.Open(connectionOrPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (!reader.Read())
            return Task.FromResult<IReadOnlyList<ColumnSchema>>(new List<ColumnSchema>());

        var headers = GetHeaders(reader);
        if (headers.Count == 0)
            return Task.FromResult<IReadOnlyList<ColumnSchema>>(new List<ColumnSchema>());

        var columnValues = new List<List<object?>>(headers.Count);
        for (int i = 0; i < headers.Count; i++)
        {
            columnValues.Add(new List<object?>());
        }

        int sampledRowCount = 0;
        while (reader.Read() && sampledRowCount < SchemaInferenceSampleRows)
        {
            ct.ThrowIfCancellationRequested();
            sampledRowCount++;
            for (int i = 0; i < headers.Count; i++)
            {
                var val = i < reader.FieldCount ? reader.GetValue(i) : null;
                columnValues[i].Add(NormalizeCellValue(val));
            }
        }

        var schemaList = new List<ColumnSchema>(headers.Count);
        for (int i = 0; i < headers.Count; i++)
        {
            var inference = TypeInferenceEngine.InferColumn(i, headers[i], columnValues[i]);
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

    private static List<string> GetHeaders(IExcelDataReader reader)
    {
        var headers = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var raw = reader.GetValue(i)?.ToString()?.Trim();
            var headerName = !string.IsNullOrWhiteSpace(raw) ? raw : $"Column{i + 1}";
            headers.Add(headerName);
        }
        return headers;
    }

    private static object? NormalizeCellValue(object? val)
    {
        if (val == null || val is DBNull) return null;
        if (val is string s && string.IsNullOrWhiteSpace(s)) return null;
        return val;
    }
}
