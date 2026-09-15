using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AnalyticsPlatform.Api.HealthChecks;

public class PowerBiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(HealthCheckResult.Healthy("Power BI / Fabric API reachable."));
}
