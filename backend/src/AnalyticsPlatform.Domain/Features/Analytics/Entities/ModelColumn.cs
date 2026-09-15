using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public enum ColumnDataType
{
    String,
    Int64,
    Decimal,
    DateTime,
    Boolean
}

public class ModelColumn : Entity
{
    public string Name { get; private set; }
    public ColumnDataType DataType { get; private set; }
    public string? FormatString { get; private set; }
    public string SourceColumn { get; private set; }

    public ModelColumn(string name, ColumnDataType dataType, string sourceColumn, string? formatString = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Column name cannot be empty.");
        if (string.IsNullOrWhiteSpace(sourceColumn))
            throw new DomainException("Source column cannot be empty.");

        Name = name;
        DataType = dataType;
        SourceColumn = sourceColumn;
        FormatString = formatString ?? GetDefaultFormat(dataType);
    }

    private static string? GetDefaultFormat(ColumnDataType dataType) => dataType switch
    {
        ColumnDataType.Int64 => "0",
        ColumnDataType.Decimal => "#,0.00",
        ColumnDataType.DateTime => "yyyy-MM-dd",
        _ => null
    };
}

