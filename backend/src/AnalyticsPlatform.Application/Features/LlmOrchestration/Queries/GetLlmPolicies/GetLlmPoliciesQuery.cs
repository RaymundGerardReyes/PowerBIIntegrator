using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Queries.GetLlmPolicies;

public sealed record GetLlmPoliciesQuery : IRequest<Result<IReadOnlyList<LlmPolicy>>>;

