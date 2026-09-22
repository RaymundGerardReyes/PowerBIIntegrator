using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Dashboards.Entities;

public static class VisualTypes
{
    public const string BarChart = "barChart";
    public const string ColumnChart = "columnChart";
    public const string LineChart = "lineChart";
    public const string Table = "tableEx";
    public const string Matrix = "matrix";
    public const string Card = "card";
    public const string DonutChart = "donutChart";
    public const string PieChart = "pieChart";
    public const string AreaChart = "areaChart";
}

public sealed record VisualLayout(double X, double Y, double Width, double Height, int ZOrder, bool Visible);

public sealed record VisualFieldBinding(string Table, string Field, bool IsMeasure, string? QueryRef = null)
{
    public string ComputedQueryRef => QueryRef ?? $"{Table}.{Field}";
}

public sealed record VisualQueryBinding(
    IReadOnlyList<VisualFieldBinding> Categories,
    IReadOnlyList<VisualFieldBinding> Values,
    IReadOnlyList<VisualFieldBinding>? Series = null,
    IReadOnlyList<VisualFieldBinding>? Tooltips = null);

public class Visual : Entity
{
    private static readonly char[] FieldSplitChars = ['.', '[', ']'];

    public string VisualType { get; private set; }
    public string Name { get; private set; }
    public VisualLayout Layout { get; private set; }
    public IReadOnlyList<string> BoundFields { get; private set; }
    public VisualQueryBinding? QueryBinding { get; private set; }

    public Visual(string visualType, string name, VisualLayout layout, IReadOnlyList<string> boundFields, VisualQueryBinding? queryBinding = null)
    {
        VisualType = visualType;
        Name = name;
        Layout = layout;
        BoundFields = boundFields;
        QueryBinding = queryBinding ?? CreateDefaultBinding(boundFields, visualType);
    }

    public void UpdateLayout(VisualLayout newLayout) => Layout = newLayout;
    public void SetQueryBinding(VisualQueryBinding queryBinding) => QueryBinding = queryBinding;

    public static bool IsMeasureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var lower = name.ToLowerInvariant();

        if (lower == "totalrows" || lower == "total_rows" || lower.StartsWith("totalrows_", StringComparison.Ordinal))
            return true;

        if (lower.StartsWith("total_", StringComparison.Ordinal) || lower.StartsWith("total", StringComparison.Ordinal) ||
            lower.StartsWith("sum_", StringComparison.Ordinal) || lower.StartsWith("sum", StringComparison.Ordinal) ||
            lower.StartsWith("average_", StringComparison.Ordinal) || lower.StartsWith("average", StringComparison.Ordinal) ||
            lower.StartsWith("avg_", StringComparison.Ordinal) || lower.StartsWith("avg", StringComparison.Ordinal))
            return true;

        if (lower.EndsWith("_rate", StringComparison.Ordinal) || lower.EndsWith("rate", StringComparison.Ordinal) ||
            lower.EndsWith("_pct", StringComparison.Ordinal) || lower.EndsWith("_percent", StringComparison.Ordinal) ||
            lower.EndsWith("percentage", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static VisualQueryBinding CreateDefaultBinding(IReadOnlyList<string> boundFields, string visualType = "")
    {
        var categories = new List<VisualFieldBinding>();
        var values = new List<VisualFieldBinding>();

        var isTable = visualType.Equals(VisualTypes.Table, StringComparison.OrdinalIgnoreCase) ||
                      visualType.Equals("table", StringComparison.OrdinalIgnoreCase);
        var isCard = visualType.Equals(VisualTypes.Card, StringComparison.OrdinalIgnoreCase);

        foreach (var field in boundFields)
        {
            var parts = field.Split(FieldSplitChars, StringSplitOptions.RemoveEmptyEntries);
            var table = parts.Length > 0 ? parts[0] : "Data";
            var col = parts.Length > 1 ? parts[1] : field;

            var isMeasure = IsMeasureName(col);

            if (isTable || isCard)
            {
                // For tables (tableEx) and cards, all projections map to Values role per ADR 0005
                values.Add(new VisualFieldBinding(table, col, IsMeasure: isMeasure));
            }
            else if (isMeasure)
            {
                values.Add(new VisualFieldBinding(table, col, IsMeasure: true));
            }
            else
            {
                categories.Add(new VisualFieldBinding(table, col, IsMeasure: false));
            }
        }

        return new VisualQueryBinding(categories, values);
    }
}
