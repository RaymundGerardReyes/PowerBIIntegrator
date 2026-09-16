using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.ImportPowerBiArtifact;

public class ImportPowerBiArtifactCommandHandler : IRequestHandler<ImportPowerBiArtifactCommand, Result<ImportPowerBiArtifactResponse>>
{
    private readonly IPowerBiPublisher _publisher;

    public ImportPowerBiArtifactCommandHandler(IPowerBiPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<Result<ImportPowerBiArtifactResponse>> Handle(ImportPowerBiArtifactCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            return Result<ImportPowerBiArtifactResponse>.Failure("WorkspaceId cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.DatasetDisplayName))
            return Result<ImportPowerBiArtifactResponse>.Failure("DatasetDisplayName cannot be empty.");

        if (request.ArtifactStream == null || request.ArtifactStream.Length == 0)
            return Result<ImportPowerBiArtifactResponse>.Failure("Artifact stream cannot be null or empty.");

        var importId = await _publisher.ImportArtifactAsync(
            request.WorkspaceId,
            request.DatasetDisplayName,
            request.ArtifactStream,
            request.FileName,
            cancellationToken);

        return Result<ImportPowerBiArtifactResponse>.Success(new ImportPowerBiArtifactResponse(
            importId,
            request.WorkspaceId,
            request.DatasetDisplayName,
            request.FileName));
    }
}

