using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IPowerBiPublisher
{
    Task<string> PublishAsync(DashboardDefinition dashboard, string workspaceId, CancellationToken ct = default);
    Task<string> ImportArtifactAsync(string workspaceId, string datasetDisplayName, Stream artifactStream, string fileName, CancellationToken ct = default);
}
