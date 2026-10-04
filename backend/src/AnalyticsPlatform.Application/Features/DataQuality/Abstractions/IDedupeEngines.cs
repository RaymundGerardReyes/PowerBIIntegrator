using AnalyticsPlatform.Domain.Features.DataQuality.Entities;

namespace AnalyticsPlatform.Application.Features.DataQuality.Abstractions;

public interface IHashDedupeEngine
{
    (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) Deduplicate(TabularBatch batch);
}

public interface ICompositeKeyDedupeEngine
{
    (TabularBatch CleanBatch, IReadOnlyList<DuplicateCluster> Clusters) Deduplicate(TabularBatch batch, IReadOnlyList<string> keyColumns);
}
