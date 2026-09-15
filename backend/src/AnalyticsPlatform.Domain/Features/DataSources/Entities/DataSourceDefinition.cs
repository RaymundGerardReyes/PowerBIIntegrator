using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataSources.Entities;

public enum DataSourceType { Excel, Csv, SqlServer, PostgreSql, MySql }

public class DataSourceDefinition : Entity
{
    private readonly List<ColumnSchema> _schema = [];

    public string Name { get; private set; }
    public DataSourceType Type { get; private set; }
    public string ConnectionOrPath { get; private set; }

    /// <summary>Schema columns extracted from the data source at registration time.</summary>
    public IReadOnlyList<ColumnSchema> Schema => _schema.AsReadOnly();

    public DataSourceDefinition(string name, DataSourceType type, string connectionOrPath)
    {
        Name = name;
        Type = type;
        ConnectionOrPath = connectionOrPath;
    }

    /// <summary>Attaches an extracted column schema to this data source definition.</summary>
    public void SetSchema(IReadOnlyList<ColumnSchema> schema)
    {
        _schema.Clear();
        _schema.AddRange(schema);
    }
}
