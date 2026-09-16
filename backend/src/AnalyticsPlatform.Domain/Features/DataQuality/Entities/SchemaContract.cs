using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public record ColumnContract(
    string ColumnName,
    string ExpectedType,
    bool IsNullable,
    bool IsKeyColumn
);

public class SchemaContract : Entity
{
    public string TableName { get; private set; }
    public int Version { get; private set; }
    public List<ColumnContract> Columns { get; } = new();

    public SchemaContract(string tableName, int version = 1)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new DomainException("Table name cannot be empty.");

        TableName = tableName;
        Version = version;
    }

    public void AddColumn(ColumnContract column)
    {
        Columns.Add(column);
    }
}

