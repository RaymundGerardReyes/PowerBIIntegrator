namespace AnalyticsPlatform.Domain.Features.Analytics.Rules;

public static class MeasureValidationRules
{
    private static readonly string[] ForbiddenTokens = { ";", "--", "DROP", "EXEC" };

    public static bool IsExpressionSafe(string expression)
        => !ForbiddenTokens.Any(token =>
            expression.Contains(token, StringComparison.OrdinalIgnoreCase));
}
