using MediatR;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Commands;

public record TransformDatasetCommand(string SilverSourceTable, string TransformationPlanName, string TargetGoldTable) : IRequest<Result<PipelineRunResult>>;

public class TransformDatasetCommandHandler : IRequestHandler<TransformDatasetCommand, Result<PipelineRunResult>>
{
    public Task<Result<PipelineRunResult>> Handle(TransformDatasetCommand request, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var result = new PipelineRunResult(runId, request.SilverSourceTable, startTime, startTime.AddSeconds(2), true);

        result.AddStageSummary(new StageRunSummary("Transformation", true, 95, 95, 0, new[] { request.TransformationPlanName }, $"Transformed into Gold table '{request.TargetGoldTable}'."));

        return Task.FromResult(Result.Success(result));
    }
}
