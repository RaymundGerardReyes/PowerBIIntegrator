using System.Text.RegularExpressions;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsDataType = AnalyticsPlatform.Domain.Features.Analytics.Entities.ColumnDataType;
using SourceDataType = AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType;

namespace AnalyticsPlatform.Application.Features.Analytics.Services;

public static class AnalyticsModelFactory
{
    public static AnalyticsModel CreateFromDataSource(
        string datasetName,
        IReadOnlyList<ColumnSchema>? schema,
        Guid? modelId = null)
    {
        var sanitizedName = SanitizeIdentifier(datasetName);
        var id = modelId ?? GenerateDeterministicGuid(datasetName);
        var model = new AnalyticsModel(id, $"{sanitizedName} Semantic Model");

        var tableName = sanitizedName;
        if (string.IsNullOrWhiteSpace(tableName))
        {
            tableName = "Dataset";
        }

        var table = model.GetOrAddTable(tableName);

        if (schema != null && schema.Count > 0)
        {
            foreach (var col in schema)
            {
                var colName = SanitizeIdentifier(col.Name);
                if (string.IsNullOrWhiteSpace(colName)) continue;

                var mappedType = MapDataType(col.InferredType);
                table.AddColumn(new ModelColumn(colName, mappedType, col.Name));
            }
        }
        else
        {
            table.AddColumn(new ModelColumn("Id", AnalyticsDataType.Int64, "Id"));
            table.AddColumn(new ModelColumn("Value", AnalyticsDataType.String, "Value"));
        }

        // 1. Primary Row Count Measure (Standard across all tabular models)
        var countExpr = new MeasureExpression($"COUNTROWS('{tableName}')", "integer");
        var countMeasure = Measure.Create("TotalRows", countExpr, tableName);
        if (countMeasure.IsSuccess && countMeasure.Value != null)
        {
            table.AddMeasure(countMeasure.Value);
            model.AddMeasure(countMeasure.Value);
        }

        // 2. Intelligent Domain Measure Synthesis
        foreach (var col in table.Columns)
        {
            var lowerName = col.Name.ToLowerInvariant();

            // Exclude ID and ordinal categorical indicators from additive summing
            if (lowerName is "id" or "passengerid" or "customerid" or "orderid" or "pclass" or "class" or "ticket" ||
                lowerName.EndsWith("id", StringComparison.Ordinal) ||
                lowerName.EndsWith("code", StringComparison.Ordinal) ||
                lowerName.EndsWith("key", StringComparison.Ordinal))
            {
                continue;
            }

            // Financial & Currency metrics -> SUM and AVERAGE
            if (lowerName.Contains("fare", StringComparison.Ordinal) ||
                lowerName.Contains("revenue", StringComparison.Ordinal) ||
                lowerName.Contains("sales", StringComparison.Ordinal) ||
                lowerName.Contains("amount", StringComparison.Ordinal) ||
                lowerName.Contains("price", StringComparison.Ordinal) ||
                lowerName.Contains("cost", StringComparison.Ordinal) ||
                lowerName.Contains("margin", StringComparison.Ordinal) ||
                lowerName.Contains("spend", StringComparison.Ordinal) ||
                lowerName.Contains("salary", StringComparison.Ordinal))
            {
                var sumExpr = new MeasureExpression($"SUM('{tableName}'[{col.Name}])", "decimal");
                var sumMeasure = Measure.Create($"Total_{col.Name}", sumExpr, tableName);
                if (sumMeasure.IsSuccess && sumMeasure.Value != null)
                {
                    table.AddMeasure(sumMeasure.Value);
                    model.AddMeasure(sumMeasure.Value);
                }

                var avgExpr = new MeasureExpression($"AVERAGE('{tableName}'[{col.Name}])", "decimal");
                var avgMeasure = Measure.Create($"Average_{col.Name}", avgExpr, tableName);
                if (avgMeasure.IsSuccess && avgMeasure.Value != null)
                {
                    table.AddMeasure(avgMeasure.Value);
                    model.AddMeasure(avgMeasure.Value);
                }
                continue;
            }

            // Binary & Classification Rates -> AVERAGE percentage
            if (lowerName is "survived" or "target" or "churn" or "is_active" or "active" or "converted" or "success")
            {
                var rateExpr = new MeasureExpression($"AVERAGE('{tableName}'[{col.Name}])", "decimal");
                var rateMeasure = Measure.Create($"{col.Name}_Rate", rateExpr, tableName);
                if (rateMeasure.IsSuccess && rateMeasure.Value != null)
                {
                    table.AddMeasure(rateMeasure.Value);
                    model.AddMeasure(rateMeasure.Value);
                }
                continue;
            }

            // Continuous measurements -> AVERAGE
            if (lowerName.Contains("age", StringComparison.Ordinal) ||
                lowerName.Contains("score", StringComparison.Ordinal) ||
                lowerName.Contains("duration", StringComparison.Ordinal) ||
                lowerName.Contains("rating", StringComparison.Ordinal) ||
                lowerName.Contains("quantity", StringComparison.Ordinal) ||
                lowerName.Contains("qty", StringComparison.Ordinal) ||
                lowerName.Contains("weight", StringComparison.Ordinal) ||
                lowerName.Contains("height", StringComparison.Ordinal))
            {
                var avgExpr = new MeasureExpression($"AVERAGE('{tableName}'[{col.Name}])", "decimal");
                var avgMeasure = Measure.Create($"Average_{col.Name}", avgExpr, tableName);
                if (avgMeasure.IsSuccess && avgMeasure.Value != null)
                {
                    table.AddMeasure(avgMeasure.Value);
                    model.AddMeasure(avgMeasure.Value);
                }
                continue;
            }

            // Fallback for general numeric columns that are non-identifiers
            if (col.DataType is AnalyticsDataType.Decimal or AnalyticsDataType.Int64)
            {
                var sumExpr = new MeasureExpression($"SUM('{tableName}'[{col.Name}])", col.DataType == AnalyticsDataType.Decimal ? "decimal" : "integer");
                var sumMeasure = Measure.Create($"Total_{col.Name}", sumExpr, tableName);
                if (sumMeasure.IsSuccess && sumMeasure.Value != null)
                {
                    table.AddMeasure(sumMeasure.Value);
                    model.AddMeasure(sumMeasure.Value);
                }
            }
        }

        return model;
    }

    public static AnalyticsDataType MapDataType(SourceDataType source) => source switch
    {
        SourceDataType.Integer => AnalyticsDataType.Int64,
        SourceDataType.Decimal => AnalyticsDataType.Decimal,
        SourceDataType.Boolean => AnalyticsDataType.Boolean,
        SourceDataType.DateTime => AnalyticsDataType.DateTime,
        _ => AnalyticsDataType.String
    };

    public static string SanitizeIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Table";

        var withoutExt = input;
        foreach (var ext in new[] { ".xls", ".xlsx", ".csv", ".tsv", ".json", ".pbip", ".pbix" })
        {
            if (withoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                withoutExt = withoutExt[..^ext.Length];
                break;
            }
        }

        // If prefixed by digits and space (e.g. "6 titanic" or "01_data"), strip the file index
        var trimmed = Regex.Replace(withoutExt, @"^[0-9]+[\s_]+", "");
        if (string.IsNullOrWhiteSpace(trimmed)) trimmed = withoutExt;

        var clean = Regex.Replace(trimmed, @"[^a-zA-Z0-9_]", "_");
        clean = Regex.Replace(clean, @"_+", "_").Trim('_');

        if (clean.Length > 0 && char.IsDigit(clean[0]))
        {
            clean = "T_" + clean;
        }

        return string.IsNullOrWhiteSpace(clean) ? "Table" : clean;
    }

    public static Guid GenerateDeterministicGuid(string input)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
