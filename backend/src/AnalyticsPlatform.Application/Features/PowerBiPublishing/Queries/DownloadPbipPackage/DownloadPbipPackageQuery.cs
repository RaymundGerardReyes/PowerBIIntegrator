using MediatR;
using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.DownloadPbipPackage;

public sealed record PbipPackageDownloadResponse(string FileName, byte[] ZipBytes, string ContentType);

public sealed record DownloadPbipPackageQuery(
    Guid DashboardDefinitionId,
    Guid AnalyticsModelId,
    string? ProjectName = null) : IRequest<Result<PbipPackageDownloadResponse>>;

