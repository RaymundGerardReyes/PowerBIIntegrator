namespace AnalyticsPlatform.Domain.Features.DataSources.Rules;

public static class SchemaCompatibilityRules
{
    public static bool ColumnTypesCompatible(string sourceType, string targetType)
        => string.Equals(sourceType, targetType, StringComparison.OrdinalIgnoreCase)
        || (sourceType == "int" && targetType == "decimal");
}
