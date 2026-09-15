namespace AnalyticsPlatform.Domain.Features.DataSources.Entities;

/// <summary>Inferred CLR-equivalent type for a data source column.</summary>
public enum ColumnDataType
{
    String,
    Integer,
    Decimal,
    Boolean,
    DateTime,
    Unknown
}

/// <summary>Immutable schema descriptor for a single column in a registered data source.</summary>
public sealed record ColumnSchema(
    int Ordinal,
    string Name,
    ColumnDataType InferredType,
    bool IsNullable,
    IReadOnlyList<string> SampleValues);

