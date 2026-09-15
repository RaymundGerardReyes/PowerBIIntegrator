using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;

public class CsvDataSourceReader : IDataSourceReader, IDataSourceSchemaExtractor
{
    private const int SchemaInferenceSampleRows = 200;

    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null,
        BadDataFound = null
    };

    public async Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            return new List<IDictionary<string, object?>>();

        using var reader = new StreamReader(connectionOrPath);
        using var csv = new CsvReader(reader, CsvConfig);

        var rows = new List<IDictionary<string, object?>>();
        if (!await csv.ReadAsync())
            return rows;

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header);
            }
            rows.Add(record);
        }

        return rows;
    }

    public async IAsyncEnumerable<IReadOnlyList<IDictionary<string, object?>>> ReadBatchesAsync(
        string connectionOrPath,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            yield break;

        using var reader = new StreamReader(connectionOrPath);
        using var csv = new CsvReader(reader, CsvConfig);

        if (!await csv.ReadAsync())
            yield break;

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var currentBatch = new List<IDictionary<string, object?>>(batchSize);

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header);
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
    }

    public async Task<IReadOnlyList<ColumnSchema>> ExtractSchemaAsync(string connectionOrPath, CancellationToken ct = default)
    {
        ValidatePath(connectionOrPath);

        if (!File.Exists(connectionOrPath))
            return new List<ColumnSchema>();

        using var reader = new StreamReader(connectionOrPath);
        using var csv = new CsvReader(reader, CsvConfig);

        if (!await csv.ReadAsync())
            return new List<ColumnSchema>();

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        if (headers.Length == 0)
            return new List<ColumnSchema>();

        var columnValues = new List<List<object?>>(headers.Length);
        for (int i = 0; i < headers.Length; i++)
        {
            columnValues.Add(new List<object?>());
        }

        int sampledRowCount = 0;
        while (await csv.ReadAsync() && sampledRowCount < SchemaInferenceSampleRows)
        {
            ct.ThrowIfCancellationRequested();
            sampledRowCount++;
            for (int i = 0; i < headers.Length; i++)
            {
                columnValues[i].Add(csv.GetField(i));
            }
        }

        var schemaList = new List<ColumnSchema>(headers.Length);
        for (int i = 0; i < headers.Length; i++)
        {
            var inference = TypeInferenceEngine.InferColumn(i, headers[i], columnValues[i]);
            schemaList.Add(inference.Schema);
        }

        return schemaList;
    }

    private static void ValidatePath(string connectionOrPath)
    {
        if (string.IsNullOrWhiteSpace(connectionOrPath))
            throw new ArgumentException("Path cannot be empty.", nameof(connectionOrPath));

        if (connectionOrPath.Contains(".."))
            throw new InvalidOperationException($"Potential directory traversal detected in path: '{connectionOrPath}'.");
    }
}
