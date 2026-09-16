using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public interface ILlmPolicyRepository
{
    Task<LlmPolicy> GetPolicyByIdAsync(string policyId, CancellationToken ct);
    Task<IReadOnlyList<LlmPolicy>> GetAllPoliciesAsync(CancellationToken ct);
    Task RegisterPolicyAsync(LlmPolicy policy, CancellationToken ct);
}
