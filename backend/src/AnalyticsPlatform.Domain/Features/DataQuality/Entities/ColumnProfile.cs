namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public record ColumnProfile(
    string ColumnName,
    string InferredType,
    long TotalRowCount,
    long NullCount,
    double NullRatio,
    long DistinctCount,
    string? MinValue,
    string? MaxValue,
    IReadOnlyList<string> TopValues,
    string DetectedPatternRegex,
    string CardinalityClass
);

