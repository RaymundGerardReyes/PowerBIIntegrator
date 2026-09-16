using MediatR;
using Microsoft.Extensions.Logging;

namespace AnalyticsPlatform.Application.Features.DataQuality.Behaviors;

public class PipelineRunAuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PipelineRunAuditBehavior<TRequest, TResponse>> _logger;

    public PipelineRunAuditBehavior(ILogger<PipelineRunAuditBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        if (requestName.Contains("DataQuality", StringComparison.OrdinalIgnoreCase) || requestName.Contains("Pipeline", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[DQTE Pipeline Audit] Starting execution of {RequestName}", requestName);
        }

        var response = await next();

        if (requestName.Contains("DataQuality", StringComparison.OrdinalIgnoreCase) || requestName.Contains("Pipeline", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[DQTE Pipeline Audit] Completed execution of {RequestName}", requestName);
        }

        return response;
    }
}

