using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Rules;

public record ChartSuggestion(
    string RecommendedVisualType,
    double ConfidenceScore,
    string Reason
);

public static class VisualMappingRule
{
    public static IReadOnlyList<ChartSuggestion> MapSuggestions(DatasetProfile profile)
    {
        var suggestions = new List<ChartSuggestion>();

        var dateCols = profile.ColumnProfiles.Where(c => c.InferredType.Equals("DateTime", StringComparison.OrdinalIgnoreCase)).ToList();
        var numericCols = profile.ColumnProfiles.Where(c => c.InferredType.Equals("Decimal", StringComparison.OrdinalIgnoreCase) || c.InferredType.Equals("Int64", StringComparison.OrdinalIgnoreCase)).ToList();
        var categoryCols = profile.ColumnProfiles.Where(c => c.InferredType.Equals("String", StringComparison.OrdinalIgnoreCase) && c.CardinalityClass == "Low").ToList();

        if (dateCols.Count > 0 && numericCols.Count > 0)
        {
            suggestions.Add(new ChartSuggestion("lineChart", 0.95, $"Detected datetime column '{dateCols[0].ColumnName}' and numeric column '{numericCols[0].ColumnName}' -> Line Chart recommended for time-series trend analysis."));
        }

        if (categoryCols.Count > 0 && numericCols.Count > 0)
        {
            suggestions.Add(new ChartSuggestion("barChart", 0.90, $"Detected low-cardinality category '{categoryCols[0].ColumnName}' and numeric measure '{numericCols[0].ColumnName}' -> Bar/Column Chart recommended."));
        }

        if (numericCols.Count >= 2)
        {
            suggestions.Add(new ChartSuggestion("scatterPlot", 0.85, $"Detected multiple numeric measures ('{numericCols[0].ColumnName}', '{numericCols[1].ColumnName}') -> Scatter Plot recommended for correlation testing."));
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new ChartSuggestion("table", 1.0, "Default tabular grid view for raw or unclassified tabular structures."));
        }

        return suggestions;
    }
}

