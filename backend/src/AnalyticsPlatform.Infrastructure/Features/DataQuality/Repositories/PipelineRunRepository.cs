using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Infrastructure.Features.DataQuality.Repositories;

public class PipelineRunRepository
{
    private readonly List<PipelineRunResult> _runs = new();

    public Task SaveRunResultAsync(PipelineRunResult result, CancellationToken ct = default)
    {
        _runs.Add(result);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PipelineRunResult>> GetRunHistoryAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<PipelineRunResult>>(_runs);
    }
}

