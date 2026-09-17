using MediatR;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Commands.RunAdvisoryQuery;

public sealed record RunAdvisoryQueryCommand(
    AdvisoryQueryRequest Request
) : IRequest<Result<AdvisoryResultDto>>;

