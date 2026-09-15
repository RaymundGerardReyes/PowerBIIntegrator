using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public enum RelationshipCrossFiltering
{
    Single,
    Both
}

public class ModelRelationship : Entity
{
    public string Name { get; private set; }
    public string FromTable { get; private set; }
    public string FromColumn { get; private set; }
    public string ToTable { get; private set; }
    public string ToColumn { get; private set; }
    public RelationshipCrossFiltering CrossFiltering { get; private set; }
    public bool IsActive { get; private set; }

    public ModelRelationship(
        string name,
        string fromTable,
        string fromColumn,
        string toTable,
        string toColumn,
        RelationshipCrossFiltering crossFiltering = RelationshipCrossFiltering.Single,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Relationship name cannot be empty.");
        if (string.IsNullOrWhiteSpace(fromTable) || string.IsNullOrWhiteSpace(fromColumn))
            throw new DomainException("From table and column must be specified.");
        if (string.IsNullOrWhiteSpace(toTable) || string.IsNullOrWhiteSpace(toColumn))
            throw new DomainException("To table and column must be specified.");

        Name = name;
        FromTable = fromTable;
        FromColumn = fromColumn;
        ToTable = toTable;
        ToColumn = toColumn;
        CrossFiltering = crossFiltering;
        IsActive = isActive;
    }
}

