using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Common;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;

public sealed record RunLlmTaskCommand(
    string TaskType,
    string UserPrompt,
    IReadOnlyList<string> ContextIds,
    LlmProviderType ProviderPreference,
    SensitivityLevel Sensitivity,
    string PolicyId,
    string CorrelationId
) : IRequest<Result<LlmTaskResult>>, ILlmGuardedRequest
{
    public string SanitizedPrompt { get; private set; } = UserPrompt;

    public void UpdateSanitizedPrompt(string sanitizedPrompt)
    {
        SanitizedPrompt = sanitizedPrompt;
    }
}
