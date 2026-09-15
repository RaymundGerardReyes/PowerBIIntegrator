using System.Buffers;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;

public class SqlServerConnector : IDataSourceReader, IDataSourceSchemaExtractor
{
    private static readonly SearchValues<char> TableNameDelimiters = SearchValues.Create([' ', ';', '\r', '\n']);
    public async Task<IReadOnlyList<IDictionary<string, object?>>> ReadAsync(string connectionOrPath, CancellationToken ct = default)
    {
        var (connectionString, query) = ParseConnectionStringAndQuery(connectionOrPath);

        var rows = new List<IDictionary<string, object?>>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = query;
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                record[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(record);
        }

        return rows;
    }

    public async IAsyncEnumerable<IReadOnlyList<IDictionary<string, object?>>> ReadBatchesAsync(
        string connectionOrPath,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (connectionString, query) = ParseConnectionStringAndQuery(connectionOrPath);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = query;
        await using var reader = await command.ExecuteReaderAsync(ct);

        var currentBatch = new List<IDictionary<string, object?>>(batchSize);

        while (await reader.ReadAsync(ct))
        {
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                record[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
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
        var (connectionString, query) = ParseConnectionStringAndQuery(connectionOrPath);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // Strategy 1: Attempt GetSchemaTable() on reader
        var schemaFromTable = await TryExtractUsingSchemaTableAsync(connection, query, ct);
        if (schemaFromTable != null && schemaFromTable.Count > 0)
        {
            return schemaFromTable;
        }

        // Strategy 2: Fallback to INFORMATION_SCHEMA.COLUMNS
        var tableName = ExtractTableNameFromQuery(query);
        return await ExtractUsingInformationSchemaAsync(connection, tableName, ct);
    }

    private static async Task<IReadOnlyList<ColumnSchema>?> TryExtractUsingSchemaTableAsync(
        SqlConnection connection,
        string query,
        CancellationToken ct)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = query;
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly, ct);

            var schemaTable = reader.GetSchemaTable();
            if (schemaTable == null)
                return null;

            var result = new List<ColumnSchema>();
            int fallbackOrdinal = 0;

            foreach (DataRow row in schemaTable.Rows)
            {
                var colName = row["ColumnName"]?.ToString() ?? $"Column_{fallbackOrdinal}";
                var dataType = row["DataType"] as Type ?? typeof(string);
                var isNullable = row["AllowDBNull"] is bool b && b;
                var ordinal = row["ColumnOrdinal"] is int o ? o : fallbackOrdinal;

                var inferredType = MapSystemTypeToColumnDataType(dataType);
                result.Add(new ColumnSchema(ordinal, colName, inferredType, isNullable, Array.Empty<string>()));
                fallbackOrdinal++;
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<IReadOnlyList<ColumnSchema>> ExtractUsingInformationSchemaAsync(
        SqlConnection connection,
        string? tableName,
        CancellationToken ct)
    {
        var result = new List<ColumnSchema>();

        await using var command = connection.CreateCommand();
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            command.CommandText = @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName 
                ORDER BY ORDINAL_POSITION";
            command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar) { Value = tableName });
        }
        else
        {
            command.CommandText = @"
                SELECT TOP 50 COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION 
                FROM INFORMATION_SCHEMA.COLUMNS 
                ORDER BY TABLE_NAME, ORDINAL_POSITION";
        }

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var colName = reader.GetString(0);
            var sqlDataType = reader.GetString(1);
            var isNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase);
            var ordinal = reader.GetInt32(3);

            var inferred = MapSqlTypeNameToColumnDataType(sqlDataType);
            result.Add(new ColumnSchema(ordinal, colName, inferred, isNullable, Array.Empty<string>()));
        }

        return result;
    }

    private static (string ConnectionString, string Query) ParseConnectionStringAndQuery(string connectionOrPath)
    {
        if (string.IsNullOrWhiteSpace(connectionOrPath))
            throw new ArgumentNullException(nameof(connectionOrPath), "SQL connection string cannot be null or empty.");

        // Check if connectionOrPath has query delimiter (|)
        var parts = connectionOrPath.Split('|', 2, StringSplitOptions.TrimEntries);
        var connectionString = parts[0];
        var query = parts.Length > 1 ? parts[1] : "SELECT 1 AS Status";

        return (connectionString, query);
    }

    private static string? ExtractTableNameFromQuery(string query)
    {
        var fromIndex = query.IndexOf("FROM ", StringComparison.OrdinalIgnoreCase);
        if (fromIndex < 0) return null;

        var afterFrom = query[(fromIndex + 5)..].Trim();
        var spaceIndex = afterFrom.AsSpan().IndexOfAny(TableNameDelimiters);
        var rawTable = spaceIndex > 0 ? afterFrom[..spaceIndex] : afterFrom;
        return rawTable.Trim('[', ']');
    }

    public static ColumnDataType MapSystemTypeToColumnDataType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(bool))
            return ColumnDataType.Boolean;
        if (actualType == typeof(byte) || actualType == typeof(sbyte) || actualType == typeof(short) ||
            actualType == typeof(ushort) || actualType == typeof(int) || actualType == typeof(uint) ||
            actualType == typeof(long) || actualType == typeof(ulong))
            return ColumnDataType.Integer;
        if (actualType == typeof(float) || actualType == typeof(double) || actualType == typeof(decimal))
            return ColumnDataType.Decimal;
        if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset) || actualType == typeof(DateOnly))
            return ColumnDataType.DateTime;

        return ColumnDataType.String;
    }

    public static ColumnDataType MapSqlTypeNameToColumnDataType(string sqlDataType)
    {
        return sqlDataType.ToLowerInvariant() switch
        {
            "bit" => ColumnDataType.Boolean,
            "tinyint" or "smallint" or "int" or "bigint" => ColumnDataType.Integer,
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => ColumnDataType.Decimal,
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => ColumnDataType.DateTime,
            _ => ColumnDataType.String
        };
    }
}
