using MediatR;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Queries.GetAdvisoryPolicies;

public sealed record GetAdvisoryPoliciesQuery : IRequest<Result<IReadOnlyList<AdvisoryPolicyDto>>>;

public sealed class GetAdvisoryPoliciesQueryHandler : IRequestHandler<GetAdvisoryPoliciesQuery, Result<IReadOnlyList<AdvisoryPolicyDto>>>
{
    public Task<Result<IReadOnlyList<AdvisoryPolicyDto>>> Handle(GetAdvisoryPoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = new List<AdvisoryPolicy>
        {
            AdvisoryPolicy.Default(),
            AdvisoryPolicy.StrictLocal(),
            AdvisoryPolicy.DataStewardElevated()
        };

        var dtos = policies.Select(p => new AdvisoryPolicyDto(
            p.PolicyName,
            p.AllowCloudProvider,
            p.MaxExposureLevel.ToString(),
            p.AllowedTools.ToList(),
            p.RequireHumanApprovalForConfidential
        )).ToList();

        return Task.FromResult(Result<IReadOnlyList<AdvisoryPolicyDto>>.Success(dtos));
    }
}

