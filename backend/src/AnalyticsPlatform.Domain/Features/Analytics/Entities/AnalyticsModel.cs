using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.Analytics.Entities;

public class AnalyticsModel : Entity
{
    public string Name { get; private set; }
    public List<Measure> Measures { get; } = new();
    public List<Dimension> Dimensions { get; } = new();
    public List<ModelTable> Tables { get; } = new();
    public List<ModelRelationship> Relationships { get; } = new();
    public string Culture { get; private set; } = "en-US";

    public AnalyticsModel(string name, string culture = "en-US")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Model name cannot be empty.");

        Name = name;
        Culture = culture;
    }

    public void AddMeasure(Measure measure) => Measures.Add(measure);
    public void AddDimension(Dimension dimension) => Dimensions.Add(dimension);

    public void AddTable(ModelTable table)
    {
        if (Tables.Any(t => t.Name.Equals(table.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Table '{table.Name}' already exists in model '{Name}'.");

        Tables.Add(table);
    }

    public void AddRelationship(ModelRelationship relationship)
    {
        Relationships.Add(relationship);
    }

    public ModelTable GetOrAddTable(string tableName)
    {
        var existing = Tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        var table = new ModelTable(tableName);
        Tables.Add(table);
        return table;
    }
}
