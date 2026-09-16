using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class FabricRestClient : IPowerBiPublisher
{
    public Task<string> PublishAsync(DashboardDefinition dashboard, string workspaceId, CancellationToken ct = default)
    {
        var reportId = $"fabric-report-{dashboard.Id:N}";
        return Task.FromResult(reportId);
    }

    public Task<string> ImportArtifactAsync(string workspaceId, string datasetDisplayName, Stream artifactStream, string fileName, CancellationToken ct = default)
    {
        var importId = $"import-{Guid.NewGuid():N}";
        return Task.FromResult(importId);
    }
}
