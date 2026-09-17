using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.ImportPowerBiArtifact;

public sealed record ImportPowerBiArtifactResponse(
    string ImportId,
    string WorkspaceId,
    string DisplayName,
    string FileName,
    string ImportState = "Succeeded",
    string? DatasetId = null,
    string? ReportId = null);

public sealed record ImportPowerBiArtifactCommand(
    string WorkspaceId,
    string DatasetDisplayName,
    Stream ArtifactStream,
    string FileName) : IRequest<Result<ImportPowerBiArtifactResponse>>;

