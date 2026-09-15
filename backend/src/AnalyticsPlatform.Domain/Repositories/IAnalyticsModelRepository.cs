using AnalyticsPlatform.Domain.Features.Analytics.Entities;

namespace AnalyticsPlatform.Domain.Repositories;

public interface IAnalyticsModelRepository
{
    Task<AnalyticsModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AnalyticsModel model, CancellationToken ct = default);
}
