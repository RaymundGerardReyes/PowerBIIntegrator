using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Rules;

public record SchemaViolation(string Column, string Issue, bool IsFatal);

public static class SchemaCompatibilityRule
{
    public static Result<IReadOnlyList<SchemaViolation>> Evaluate(SchemaContract contract, DatasetProfile profile)
    {
        var violations = new List<SchemaViolation>();

        foreach (var expectedCol in contract.Columns)
        {
            var actualCol = profile.ColumnProfiles.FirstOrDefault(c => c.ColumnName.Equals(expectedCol.ColumnName, StringComparison.OrdinalIgnoreCase));
            if (actualCol == null)
            {
                violations.Add(new SchemaViolation(expectedCol.ColumnName, "Missing required column", isFatal: !expectedCol.IsNullable));
                continue;
            }

            if (!expectedCol.IsNullable && actualCol.NullCount > 0)
            {
                violations.Add(new SchemaViolation(expectedCol.ColumnName, $"Non-nullable column contains {actualCol.NullCount} null values", isFatal: false));
            }
        }

        return Result.Success<IReadOnlyList<SchemaViolation>>(violations);
    }
}
