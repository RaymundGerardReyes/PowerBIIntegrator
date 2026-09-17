using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.GetLocalPowerBiStatus;

public sealed record GetLocalPowerBiStatusQuery : IRequest<LocalPowerBiStatus>;

public class GetLocalPowerBiStatusQueryHandler : IRequestHandler<GetLocalPowerBiStatusQuery, LocalPowerBiStatus>
{
    private readonly ILocalPowerBiDesktopService _desktopService;

    public GetLocalPowerBiStatusQueryHandler(ILocalPowerBiDesktopService desktopService)
    {
        _desktopService = desktopService;
    }

    public Task<LocalPowerBiStatus> Handle(GetLocalPowerBiStatusQuery request, CancellationToken cancellationToken)
        => _desktopService.GetStatusAsync(cancellationToken);
}

