using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Queries.GetLlmPolicies;

public sealed class GetLlmPoliciesQueryHandler : IRequestHandler<GetLlmPoliciesQuery, Result<IReadOnlyList<LlmPolicy>>>
{
    private readonly ILlmPolicyRepository _policies;

    public GetLlmPoliciesQueryHandler(ILlmPolicyRepository policies)
    {
        _policies = policies;
    }

    public async Task<Result<IReadOnlyList<LlmPolicy>>> Handle(GetLlmPoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await _policies.GetAllPoliciesAsync(cancellationToken);
        return Result<IReadOnlyList<LlmPolicy>>.Success(policies);
    }
}
