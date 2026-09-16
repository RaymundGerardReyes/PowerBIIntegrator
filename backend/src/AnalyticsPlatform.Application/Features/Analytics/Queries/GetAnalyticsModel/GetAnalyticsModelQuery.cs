using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModel;

public sealed record AnalyticsModelDto(
    Guid Id,
    string Name,
    string Culture,
    IReadOnlyList<string> Tables,
    IReadOnlyList<string> Measures,
    int RelationshipsCount);

public sealed record GetAnalyticsModelQuery(Guid AnalyticsModelId) : IRequest<Result<AnalyticsModelDto>>;

public class GetAnalyticsModelQueryHandler : IRequestHandler<GetAnalyticsModelQuery, Result<AnalyticsModelDto>>
{
    private readonly IAnalyticsModelRepository _repository;

    public GetAnalyticsModelQueryHandler(IAnalyticsModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AnalyticsModelDto>> Handle(GetAnalyticsModelQuery request, CancellationToken cancellationToken)
    {
        var model = await _repository.GetByIdAsync(request.AnalyticsModelId, cancellationToken);
        if (model == null)
        {
            return Result<AnalyticsModelDto>.Failure($"Analytics model with ID '{request.AnalyticsModelId}' was not found.");
        }

        var dto = new AnalyticsModelDto(
            model.Id,
            model.Name,
            model.Culture,
            model.Tables.Select(t => t.Name).ToList(),
            model.Measures.Select(m => m.Name).ToList(),
            model.Relationships.Count);

        return Result<AnalyticsModelDto>.Success(dto);
    }
}

