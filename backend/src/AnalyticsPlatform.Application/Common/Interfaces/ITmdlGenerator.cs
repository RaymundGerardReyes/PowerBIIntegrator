using AnalyticsPlatform.Domain.Features.Analytics.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface ITmdlGenerator
{
    IVirtualFileTree GenerateSemanticModel(AnalyticsModel model);
}

