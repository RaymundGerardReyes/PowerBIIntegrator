using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.ReportPublishing.Entities;

public class PublishRequest : Entity
{
    public Guid DashboardDefinitionId { get; private set; }
    public string TargetWorkspaceId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }

    public PublishRequest(Guid dashboardDefinitionId, string targetWorkspaceId)
    {
        DashboardDefinitionId = dashboardDefinitionId;
        TargetWorkspaceId = targetWorkspaceId;
        RequestedAtUtc = DateTime.UtcNow;
    }
}
