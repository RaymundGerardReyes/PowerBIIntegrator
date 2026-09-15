using MediatR;
using AnalyticsPlatform.Application.Common.Interfaces;

namespace AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.GetReportEmbedConfig;

public sealed record GetReportEmbedConfigQuery(string ReportId) : IRequest<EmbedConfig>;

public class GetReportEmbedConfigQueryHandler : IRequestHandler<GetReportEmbedConfigQuery, EmbedConfig>
{
    private readonly IEmbedTokenService _embedTokenService;

    public GetReportEmbedConfigQueryHandler(IEmbedTokenService embedTokenService) => _embedTokenService = embedTokenService;

    public Task<EmbedConfig> Handle(GetReportEmbedConfigQuery request, CancellationToken cancellationToken)
        => _embedTokenService.GetEmbedConfigAsync(request.ReportId, cancellationToken);
}
