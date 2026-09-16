using AnalyticsPlatform.Domain.Features.LlmOrchestration.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public interface ILlmGateway
{
    Task<LlmTaskResult> InvokeAsync(LlmTask task, LlmPolicy policy, CancellationToken ct);
}
