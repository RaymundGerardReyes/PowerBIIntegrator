namespace AnalyticsPlatform.Application.Common.Interfaces;

public record EmbedConfig(string ReportId, string EmbedUrl, string AccessToken);

public interface IEmbedTokenService
{
    Task<EmbedConfig> GetEmbedConfigAsync(string reportId, CancellationToken ct = default);
}
