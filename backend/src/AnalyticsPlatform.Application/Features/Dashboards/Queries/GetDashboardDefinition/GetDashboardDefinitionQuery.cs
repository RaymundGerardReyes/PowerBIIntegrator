using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.Dashboards.Queries.GetDashboardDefinition;

public sealed record DashboardVisualSummaryDto(Guid Id, string Title, string VisualType);
public sealed record DashboardPageSummaryDto(Guid Id, string Name, int VisualsCount);

public sealed record DashboardDefinitionDto(
    Guid Id,
    string Name,
    Guid AnalyticsModelId,
    IReadOnlyList<DashboardPageSummaryDto> Pages,
    int TotalVisuals);

public sealed record GetDashboardDefinitionQuery(Guid DashboardId) : IRequest<Result<DashboardDefinitionDto>>;

public class GetDashboardDefinitionQueryHandler : IRequestHandler<GetDashboardDefinitionQuery, Result<DashboardDefinitionDto>>
{
    private readonly IDashboardRepository _repository;

    public GetDashboardDefinitionQueryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DashboardDefinitionDto>> Handle(GetDashboardDefinitionQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await _repository.GetByIdAsync(request.DashboardId, cancellationToken);
        if (dashboard == null)
        {
            return Result<DashboardDefinitionDto>.Failure($"Dashboard with ID '{request.DashboardId}' was not found.");
        }

        var pages = dashboard.Pages.Select(p => new DashboardPageSummaryDto(
            p.Id,
            p.Name,
            p.Visuals.Count)).ToList();

        var totalVisuals = dashboard.Pages.Sum(p => p.Visuals.Count);

        var dto = new DashboardDefinitionDto(
            dashboard.Id,
            dashboard.Name,
            dashboard.AnalyticsModelId,
            pages,
            totalVisuals);

        return Result<DashboardDefinitionDto>.Success(dto);
    }
}
