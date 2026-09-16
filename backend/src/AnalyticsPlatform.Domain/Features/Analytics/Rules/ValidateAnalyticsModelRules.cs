using AnalyticsPlatform.Domain.Features.Analytics.Entities;

namespace AnalyticsPlatform.Domain.Features.Analytics.Rules;

public sealed record ModelValidationReport(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> OrphanTables,
    IReadOnlyList<string> DetectedCycles);

public static class ValidateAnalyticsModelRules
{
    public static ModelValidationReport Validate(AnalyticsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var errors = new List<string>();
        var warnings = new List<string>();
        var orphanTables = new List<string>();
        var detectedCycles = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            errors.Add("Model name cannot be empty.");
        }
        else if (model.Name.Length > 200)
        {
            errors.Add("Model name exceeds maximum length of 200 characters.");
        }

        // Rule 1: Orphan tables detection (if > 1 table)
        if (model.Tables.Count > 1)
        {
            foreach (var table in model.Tables)
            {
                bool hasRelationship = model.Relationships.Any(r =>
                    r.FromTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase) ||
                    r.ToTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase));

                if (!hasRelationship)
                {
                    orphanTables.Add(table.Name);
                    warnings.Add($"Table '{table.Name}' is an orphan table with no active relationships.");
                }
            }
        }

        // Rule 2: Active relationship cycle / loop detection
        var activeRelationships = model.Relationships.Where(r => r.IsActive).ToList();
        var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in model.Tables)
        {
            adjacency[t.Name] = new List<string>();
        }

        foreach (var rel in activeRelationships)
        {
            if (!adjacency.ContainsKey(rel.FromTable)) adjacency[rel.FromTable] = new List<string>();
            if (!adjacency.ContainsKey(rel.ToTable)) adjacency[rel.ToTable] = new List<string>();

            adjacency[rel.FromTable].Add(rel.ToTable);
            if (rel.CrossFiltering == RelationshipCrossFiltering.Both)
            {
                adjacency[rel.ToTable].Add(rel.FromTable);
            }
        }

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in adjacency.Keys)
        {
            if (!visited.Contains(node))
            {
                if (HasCycle(node, adjacency, visited, inStack, new List<string>(), detectedCycles))
                {
                    errors.Add($"Circular relationship loop detected involving table '{node}'.");
                }
            }
        }

        // Rule 3: Measure validation
        foreach (var measure in model.Measures)
        {
            if (string.IsNullOrWhiteSpace(measure.Name))
            {
                errors.Add("Measure name cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(measure.Expression?.DaxOrFormula))
            {
                errors.Add($"Measure '{measure.Name}' has an empty expression.");
            }
            else if (!MeasureValidationRules.IsExpressionSafe(measure.Expression.DaxOrFormula))
            {
                errors.Add($"Measure '{measure.Name}' contains dangerous or forbidden tokens in expression.");
            }
        }

        bool isValid = errors.Count == 0;
        return new ModelValidationReport(isValid, errors, warnings, orphanTables, detectedCycles);
    }

    private static bool HasCycle(
        string current,
        Dictionary<string, List<string>> adjacency,
        HashSet<string> visited,
        HashSet<string> inStack,
        List<string> path,
        List<string> detectedCycles)
    {
        if (inStack.Contains(current))
        {
            var cyclePath = string.Join(" -> ", path.Concat([current]));
            if (!detectedCycles.Contains(cyclePath))
            {
                detectedCycles.Add(cyclePath);
            }
            return true;
        }

        if (visited.Contains(current))
            return false;

        visited.Add(current);
        inStack.Add(current);
        path.Add(current);

        if (adjacency.TryGetValue(current, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (HasCycle(neighbor, adjacency, visited, inStack, path, detectedCycles))
                    return true;
            }
        }

        inStack.Remove(current);
        path.RemoveAt(path.Count - 1);
        return false;
    }
}
