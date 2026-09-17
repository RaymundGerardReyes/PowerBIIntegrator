using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.OpenLocalPowerBiFolder;

public sealed record OpenLocalPowerBiFolderCommand(string? FolderPath = null) : IRequest<Result<bool>>;

public class OpenLocalPowerBiFolderCommandHandler : IRequestHandler<OpenLocalPowerBiFolderCommand, Result<bool>>
{
    private readonly ILocalPowerBiDesktopService _desktopService;

    public OpenLocalPowerBiFolderCommandHandler(ILocalPowerBiDesktopService desktopService)
    {
        _desktopService = desktopService;
    }

    public Task<Result<bool>> Handle(OpenLocalPowerBiFolderCommand request, CancellationToken cancellationToken)
    {
        var opened = _desktopService.OpenFolder(request.FolderPath ?? string.Empty);
        return Task.FromResult(Result<bool>.Success(opened));
    }
}

