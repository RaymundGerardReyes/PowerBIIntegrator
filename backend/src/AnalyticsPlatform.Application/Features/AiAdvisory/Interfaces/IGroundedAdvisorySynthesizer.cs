using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Services;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

public sealed record SynthesizedAdvisoryResponse(
    string Answer,
    string ProviderUsed,
    IReadOnlyList<string> CitedRuleIds,
    IReadOnlyList<string> CitedRunIds);

public interface IGroundedAdvisorySynthesizer
{
    Task<SynthesizedAdvisoryResponse> SynthesizeExplanationAsync(
        AdvisoryQueryRequest request,
        AssembledAdvisoryContext context,
        string chosenProvider,
        CancellationToken ct = default);
}

