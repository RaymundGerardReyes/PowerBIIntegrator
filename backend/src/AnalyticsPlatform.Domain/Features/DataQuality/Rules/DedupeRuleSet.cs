namespace AnalyticsPlatform.Domain.Features.DataQuality.Rules;

public enum DedupeStrategy
{
    ExactHash,
    CompositeKey,
    SimilarityCluster
}

public record DedupeRule(
    string RuleId,
    DedupeStrategy Strategy,
    IReadOnlyList<string> TargetColumns,
    double SimilarityThreshold,
    string ResolutionPolicy
);

public record DedupeRuleSet(
    string RuleSetId,
    IReadOnlyList<DedupeRule> Rules
);

