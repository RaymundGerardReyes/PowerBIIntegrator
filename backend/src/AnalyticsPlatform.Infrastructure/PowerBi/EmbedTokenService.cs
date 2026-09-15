using AnalyticsPlatform.Application.Common.Interfaces;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class EmbedTokenService : IEmbedTokenService
{
    public Task<EmbedConfig> GetEmbedConfigAsync(string reportId, CancellationToken ct = default)
    {
        var config = new EmbedConfig(
            ReportId: reportId,
            EmbedUrl: $"https://app.powerbi.com/reportEmbed?reportId={reportId}",
            AccessToken: $"mock-embed-token-{reportId}"
        );
        return Task.FromResult(config);
    }
}
