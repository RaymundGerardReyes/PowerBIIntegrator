using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;

public sealed class AdvisoryResult : Entity
{
    public string RunId { get; private set; }
    public string QuestionType { get; private set; }
    public string UserQuestion { get; private set; }
    public string Answer { get; private set; }
    public string ProviderUsed { get; private set; }
    public IReadOnlyList<string> CitedRuleIds { get; private set; }
    public IReadOnlyList<string> CitedRunIds { get; private set; }
    public int RedactedFieldsCount { get; private set; }
    public bool IsUnlockedConfidential { get; private set; }
    public string CorrelationId { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    private AdvisoryResult(
        string runId,
        string questionType,
        string userQuestion,
        string answer,
        string providerUsed,
        IReadOnlyList<string> citedRuleIds,
        IReadOnlyList<string> citedRunIds,
        int redactedFieldsCount,
        bool isUnlockedConfidential,
        string correlationId,
        DateTime timestampUtc)
    {
        RunId = runId;
        QuestionType = questionType;
        UserQuestion = userQuestion;
        Answer = answer;
        ProviderUsed = providerUsed;
        CitedRuleIds = citedRuleIds;
        CitedRunIds = citedRunIds;
        RedactedFieldsCount = redactedFieldsCount;
        IsUnlockedConfidential = isUnlockedConfidential;
        CorrelationId = correlationId;
        TimestampUtc = timestampUtc;
    }

    public static Result<AdvisoryResult> Create(
        string runId,
        string questionType,
        string userQuestion,
        string answer,
        string providerUsed,
        IReadOnlyList<string> citedRuleIds,
        IReadOnlyList<string> citedRunIds,
        int redactedFieldsCount,
        bool isUnlockedConfidential,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return Result<AdvisoryResult>.Failure("RunId cannot be empty.");

        if (string.IsNullOrWhiteSpace(userQuestion))
            return Result<AdvisoryResult>.Failure("UserQuestion cannot be empty.");

        if (string.IsNullOrWhiteSpace(answer))
            return Result<AdvisoryResult>.Failure("Answer cannot be empty.");

        return Result<AdvisoryResult>.Success(new AdvisoryResult(
            runId,
            questionType ?? "General",
            userQuestion,
            answer,
            providerUsed ?? "LocalOllama",
            citedRuleIds ?? Array.Empty<string>(),
            citedRunIds ?? Array.Empty<string>(),
            redactedFieldsCount,
            isUnlockedConfidential,
            correlationId ?? Guid.NewGuid().ToString(),
            DateTime.UtcNow));
    }
}

