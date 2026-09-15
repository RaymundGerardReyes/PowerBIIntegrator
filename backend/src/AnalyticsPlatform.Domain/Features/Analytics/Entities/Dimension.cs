using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public class Dimension : Entity
{
    public string Name { get; private set; }
    public string ColumnName { get; private set; }
    public string TableName { get; private set; }

    public Dimension(string name, string columnName, string tableName)
    {
        Name = name;
        ColumnName = columnName;
        TableName = tableName;
    }
}
