using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public class ModelTable : Entity
{
    public string Name { get; private set; }
    public string LineageTag { get; private set; }
    public List<ModelColumn> Columns { get; } = new();
    public List<Measure> Measures { get; } = new();
    public string? MQueryPartition { get; private set; }
    public string? PartitionName { get; private set; }

    public ModelTable(string name, string? lineageTag = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Table name cannot be empty.");

        Name = name;
        LineageTag = lineageTag ?? Guid.NewGuid().ToString();
        PartitionName = $"{name}-Partition";
    }

    public void AddColumn(ModelColumn column)
    {
        if (Columns.Any(c => c.Name.Equals(column.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Column '{column.Name}' already exists in table '{Name}'.");

        Columns.Add(column);
    }

    public void AddMeasure(Measure measure)
    {
        if (Measures.Any(m => m.Name.Equals(measure.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Measure '{measure.Name}' already exists in table '{Name}'.");

        Measures.Add(measure);
    }

    public void SetMQueryPartition(string partitionName, string mQuery)
    {
        PartitionName = partitionName;
        MQueryPartition = mQuery;
    }
}

