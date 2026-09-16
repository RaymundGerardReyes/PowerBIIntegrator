using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public record TransformationStep(
    int Order,
    string OperationType,
    string TargetColumn,
    string ExpressionOrSource,
    Dictionary<string, string> Parameters
);

public class TransformationPlan : Entity
{
    public string PlanName { get; private set; }
    public string TargetGoldTable { get; private set; }
    public int Version { get; private set; }
    public List<TransformationStep> Steps { get; } = new();

    public TransformationPlan(string planName, string targetGoldTable, int version = 1)
    {
        if (string.IsNullOrWhiteSpace(planName))
            throw new DomainException("Plan name cannot be empty.");

        PlanName = planName;
        TargetGoldTable = targetGoldTable;
        Version = version;
    }

    public void AddStep(TransformationStep step) => Steps.Add(step);
}

