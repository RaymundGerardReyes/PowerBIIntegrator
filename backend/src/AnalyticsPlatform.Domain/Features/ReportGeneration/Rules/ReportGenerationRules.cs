using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.ReportGeneration.Rules;

public static class ReportGenerationRules
{
    private static readonly char[] FormulaInjectionCharacters = ['=', '+', '-', '@', '\t', '\r'];

    public static Result ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure("Report title is required.");

        if (title.Trim().Length > 200)
            return Result.Failure("Report title cannot exceed 200 characters.");

        return Result.Success();
    }

    public static Result ValidateSectionCount(int sectionCount)
    {
        if (sectionCount < 1)
            return Result.Failure("A report must contain at least one section.");

        if (sectionCount > 100)
            return Result.Failure("A report cannot contain more than 100 sections.");

        return Result.Success();
    }

    public static Result ValidateTableDimensions(IReadOnlyList<string>? headers, IReadOnlyList<IReadOnlyList<string>>? rows)
    {
        if (headers == null || headers.Count == 0)
        {
            if (rows != null && rows.Count > 0)
                return Result.Failure("Table rows cannot be specified without table headers.");

            return Result.Success();
        }

        int headerCount = headers.Count;
        if (rows != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null || rows[i].Count != headerCount)
                {
                    return Result.Failure($"Row {i + 1} column count ({rows[i]?.Count ?? 0}) does not match header count ({headerCount}).");
                }
            }
        }

        return Result.Success();
    }

    public static string SanitizeCellForFormulaInjection(string? cellValue)
    {
        if (string.IsNullOrEmpty(cellValue))
            return string.Empty;

        // If cell starts with formula characters, prefix with single quote to prevent DDE/Formula execution
        if (FormulaInjectionCharacters.Contains(cellValue[0]))
        {
            return "'" + cellValue;
        }

        return cellValue;
    }
}
