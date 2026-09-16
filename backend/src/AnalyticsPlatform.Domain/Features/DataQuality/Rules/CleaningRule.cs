namespace AnalyticsPlatform.Domain.Features.DataQuality.Rules;

public enum CleaningOperation
{
    TrimWhitespace,
    NormalizeCasing,
    ParseDateUtc,
    StripCurrency,
    DefaultOnNull,
    ClipOutliersIqr
}

public record CleaningRule(
    string ColumnPattern,
    CleaningOperation Operation,
    string? RuleParameter
);
