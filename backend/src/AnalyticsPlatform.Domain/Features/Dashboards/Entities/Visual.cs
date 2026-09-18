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
        QueryBinding = queryBinding ?? CreateDefaultBinding(boundFields);
    }

    public void UpdateLayout(VisualLayout newLayout) => Layout = newLayout;
    public void SetQueryBinding(VisualQueryBinding queryBinding) => QueryBinding = queryBinding;

    private static VisualQueryBinding CreateDefaultBinding(IReadOnlyList<string> boundFields)
    {
        var categories = new List<VisualFieldBinding>();
        var values = new List<VisualFieldBinding>();

        foreach (var field in boundFields)
        {
            var parts = field.Split(FieldSplitChars, StringSplitOptions.RemoveEmptyEntries);
            var table = parts.Length > 0 ? parts[0] : "Data";
            var col = parts.Length > 1 ? parts[1] : field;

            if (col.StartsWith("Total", StringComparison.OrdinalIgnoreCase) ||
                col.StartsWith("Sum", StringComparison.OrdinalIgnoreCase) ||
                col.StartsWith("Average", StringComparison.OrdinalIgnoreCase) ||
                col.EndsWith("_Rate", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("TotalRows", StringComparison.OrdinalIgnoreCase))
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
