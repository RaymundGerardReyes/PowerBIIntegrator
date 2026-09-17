using AnalyticsPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class EmbedTokenService : IEmbedTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmbedTokenService> _logger;

    public EmbedTokenService(IConfiguration configuration, ILogger<EmbedTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmbedConfig> GetEmbedConfigAsync(string reportId, CancellationToken ct = default)
    {
        var tenantId = _configuration["PowerBi:TenantId"];
        var clientId = _configuration["PowerBi:ClientId"];
        var clientSecret = _configuration["PowerBi:ClientSecret"];
        var workspaceId = _configuration["PowerBi:WorkspaceId"];

        var isConfigured = !string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            && !string.IsNullOrWhiteSpace(workspaceId);

        if (isConfigured && Guid.TryParse(reportId, out var reportGuid) && Guid.TryParse(workspaceId, out var workspaceGuid))
        {
            try
            {
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                var scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };
                var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct);

                var tokenCredentials = new TokenCredentials(authResult.AccessToken, "Bearer");
                using var pbiClient = new PowerBIClient(new Uri("https://api.powerbi.com/"), tokenCredentials);

                var report = await pbiClient.Reports.GetReportInGroupAsync(workspaceGuid, reportGuid, ct);
                var generateTokenRequest = new GenerateTokenRequest(TokenAccessLevel.View);
                var tokenResponse = await pbiClient.Reports.GenerateTokenInGroupAsync(workspaceGuid, reportGuid, generateTokenRequest, cancellationToken: ct);

                _logger.LogInformation("Successfully acquired live Power BI embed token for report {ReportId} in workspace {WorkspaceId}", reportId, workspaceId);

                return new EmbedConfig(
                    ReportId: report.Id.ToString(),
                    EmbedUrl: report.EmbedUrl,
                    AccessToken: tokenResponse.Token
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to acquire live Power BI embed token for report {ReportId}. Falling back to demonstration token.", reportId);
            }
        }

        return new EmbedConfig(
            ReportId: reportId,
            EmbedUrl: $"https://app.powerbi.com/reportEmbed?reportId={reportId}",
            AccessToken: $"mock-embed-token-{reportId}"
        );
    }
}
