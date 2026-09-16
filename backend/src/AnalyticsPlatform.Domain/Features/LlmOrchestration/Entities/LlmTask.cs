using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;

public class LlmTask : Entity
{
    public string TaskType { get; private set; }
    public string UserPrompt { get; private set; }
    public string SanitizedPrompt { get; private set; }
    public IReadOnlyList<string> ContextIds { get; private set; }
    public LlmProviderType RequestedProvider { get; private set; }
    public SensitivityLevel Sensitivity { get; private set; }
    public string CorrelationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private LlmTask(
        string taskType,
        string userPrompt,
        string sanitizedPrompt,
        IReadOnlyList<string> contextIds,
        LlmProviderType requestedProvider,
        SensitivityLevel sensitivity,
        string correlationId,
        DateTime createdAtUtc)
    {
        TaskType = taskType;
        UserPrompt = userPrompt;
        SanitizedPrompt = sanitizedPrompt;
        ContextIds = contextIds;
        RequestedProvider = requestedProvider;
        Sensitivity = sensitivity;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<LlmTask> Create(
        string taskType,
        string userPrompt,
        IReadOnlyList<string> contextIds,
        LlmProviderType requestedProvider,
        SensitivityLevel sensitivity,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(taskType))
            return Result<LlmTask>.Failure("TaskType cannot be empty.");

        if (string.IsNullOrWhiteSpace(userPrompt))
            return Result<LlmTask>.Failure("UserPrompt cannot be empty.");

        if (string.IsNullOrWhiteSpace(correlationId))
            return Result<LlmTask>.Failure("CorrelationId cannot be empty.");

        return Result<LlmTask>.Success(new LlmTask(
            taskType,
            userPrompt,
            userPrompt,
            contextIds ?? Array.Empty<string>(),
            requestedProvider,
            sensitivity,
            correlationId,
            DateTime.UtcNow));
    }

    public void UpdateSanitizedPrompt(string sanitizedPrompt)
    {
        if (!string.IsNullOrWhiteSpace(sanitizedPrompt))
        {
            SanitizedPrompt = sanitizedPrompt;
        }
    }
}
