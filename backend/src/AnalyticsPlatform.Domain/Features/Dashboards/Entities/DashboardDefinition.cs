using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Dashboards.Entities;

public class DashboardDefinition : Entity
{
    public string Name { get; private set; }
    public List<Page> Pages { get; } = new();
    public Guid AnalyticsModelId { get; private set; }

    public DashboardDefinition(string name, Guid analyticsModelId)
    {
        Name = name;
        AnalyticsModelId = analyticsModelId;
    }

    public void AddPage(Page page) => Pages.Add(page);
}
