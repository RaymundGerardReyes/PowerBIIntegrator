using System.Globalization;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors;

public static class TypeInferenceEngine
{
    private const int MaxSampleSize = 5;

    public static ColumnInferenceResult InferColumn(int ordinal, string name, IEnumerable<object?> rawValues)
    {
        var sampleValues = new List<string>();
        var sampleSet = new HashSet<string>(StringComparer.Ordinal);
        var seenTypes = new HashSet<ColumnDataType>();
        var isNullable = false;
        var rowCount = 0;

        foreach (var val in rawValues)
        {
            rowCount++;
            if (val == null || (val is string s && string.IsNullOrWhiteSpace(s)))
            {
                isNullable = true;
                continue;
            }

            var strVal = val is DateTime dt
                ? dt.ToString("o", CultureInfo.InvariantCulture)
                : Convert.ToString(val, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            if (sampleValues.Count < MaxSampleSize && sampleSet.Add(strVal))
            {
                sampleValues.Add(strVal);
            }

            var inferred = InferSingleValue(val, strVal);
            seenTypes.Add(inferred);
        }

        if (rowCount == 0)
        {
            isNullable = true;
        }

        var resolvedType = ResolveAggregateType(seenTypes);

        return new ColumnInferenceResult(
            new ColumnSchema(ordinal, name, resolvedType, isNullable, sampleValues),
            resolvedType);
    }

    public static ColumnDataType InferSingleValue(object? val, string strVal)
    {
        if (val is bool)
            return ColumnDataType.Boolean;
        if (val is byte or sbyte or short or ushort or int or uint or long or ulong)
            return ColumnDataType.Integer;
        if (val is float f)
            return f % 1 == 0 ? ColumnDataType.Integer : ColumnDataType.Decimal;
        if (val is double d)
            return d % 1 == 0 && d >= long.MinValue && d <= long.MaxValue ? ColumnDataType.Integer : ColumnDataType.Decimal;
        if (val is decimal dec)
            return dec % 1 == 0 && dec >= long.MinValue && dec <= long.MaxValue ? ColumnDataType.Integer : ColumnDataType.Decimal;
        if (val is DateTime or DateTimeOffset or DateOnly)
            return ColumnDataType.DateTime;

        if (bool.TryParse(strVal, out _))
            return ColumnDataType.Boolean;

        if (long.TryParse(strVal, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return ColumnDataType.Integer;

        if (decimal.TryParse(strVal, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
            return ColumnDataType.Decimal;

        if (DateTime.TryParse(strVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return ColumnDataType.DateTime;

        return ColumnDataType.String;
    }

    private static ColumnDataType ResolveAggregateType(HashSet<ColumnDataType> seenTypes)
    {
        if (seenTypes.Count == 0)
            return ColumnDataType.Unknown;

        if (seenTypes.Contains(ColumnDataType.String))
            return ColumnDataType.String;

        if (seenTypes.Contains(ColumnDataType.Decimal))
        {
            // If only decimal or decimal + integer, decimal wins
            var remaining = seenTypes.Where(t => t != ColumnDataType.Decimal && t != ColumnDataType.Integer).ToList();
            return remaining.Count == 0 ? ColumnDataType.Decimal : ColumnDataType.String;
        }

        if (seenTypes.Count == 1)
            return seenTypes.First();

        return ColumnDataType.String;
    }
}

public sealed record ColumnInferenceResult(ColumnSchema Schema, ColumnDataType DataType);
