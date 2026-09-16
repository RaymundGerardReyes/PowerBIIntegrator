using Microsoft.Extensions.Diagnostics.HealthChecks;
using AnalyticsPlatform.Infrastructure.Persistence;

namespace AnalyticsPlatform.Api.HealthChecks;

public class SqlHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public SqlHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is connected and responsive.")
                : HealthCheckResult.Degraded("Database connection failed; operating with in-memory persistence fallback.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Database health check degraded: {ex.Message}");
        }
    }
}

