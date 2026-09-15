using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IPbirGenerator
{
    IVirtualFileTree GenerateReportDefinition(DashboardDefinition dashboard, string semanticModelRelativePath = "../SemanticModel");
}

