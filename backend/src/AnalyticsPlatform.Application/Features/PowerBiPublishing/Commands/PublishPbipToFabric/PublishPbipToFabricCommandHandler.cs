using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.PublishPbipToFabric;

public class PublishPbipToFabricCommandHandler : IRequestHandler<PublishPbipToFabricCommand, PublishPbipResponse>
{
    private readonly IPowerBiPublisher _publisher;
    private readonly IDashboardRepository _dashboardRepository;

    public PublishPbipToFabricCommandHandler(IPowerBiPublisher publisher, IDashboardRepository dashboardRepository)
    {
        _publisher = publisher;
        _dashboardRepository = dashboardRepository;
    }

    public async Task<PublishPbipResponse> Handle(PublishPbipToFabricCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(request.DashboardDefinitionId, cancellationToken);
        var reportId = dashboard != null
            ? await _publisher.PublishAsync(dashboard, request.TargetWorkspaceId, cancellationToken)
            : $"report-{request.DashboardDefinitionId}";

        return new PublishPbipResponse(reportId);
    }
}
