using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IPbipCompiler
{
    IVirtualFileTree CompileProject(string projectName, DashboardDefinition dashboard, AnalyticsModel model);
}

