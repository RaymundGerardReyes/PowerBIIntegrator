namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Common;

public interface ILlmGuardedRequest
{
    string PolicyId { get; }
    string UserPrompt { get; }
    IReadOnlyList<string> ContextIds { get; }
    string CorrelationId { get; }
    void UpdateSanitizedPrompt(string sanitizedPrompt);
}

