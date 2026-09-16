using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public class DuplicateCluster : Entity
{
    public string ClusterId { get; private set; }
    public string RuleFired { get; private set; }
    public string KeptRowId { get; private set; }
    public List<string> DroppedRowIds { get; } = new();
    public double ConfidenceScore { get; private set; }
    public string ReasonCode { get; private set; }

    public DuplicateCluster(string clusterId, string ruleFired, string keptRowId, IEnumerable<string> droppedRowIds, double confidenceScore, string reasonCode)
    {
        ClusterId = clusterId;
        RuleFired = ruleFired;
        KeptRowId = keptRowId;
        DroppedRowIds.AddRange(droppedRowIds);
        ConfidenceScore = confidenceScore;
        ReasonCode = reasonCode;
    }
}
