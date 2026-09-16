using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.ReportGeneration.Entities;

public sealed record ReportKpiSummary(
    string Label,
    string Value,
    string? DeltaPercentage = null,
    string? Description = null);

public sealed class ReportSection : Entity
{
    public string Title { get; private set; }
    public string? Narrative { get; private set; }
    public IReadOnlyList<ReportKpiSummary> Kpis { get; private set; }
    public IReadOnlyList<string> TableHeaders { get; private set; }
    public IReadOnlyList<IReadOnlyList<string>> TableRows { get; private set; }

    public ReportSection(
        string title,
        string? narrative = null,
        IEnumerable<ReportKpiSummary>? kpis = null,
        IEnumerable<string>? tableHeaders = null,
        IEnumerable<IEnumerable<string>>? tableRows = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Section title cannot be empty or whitespace.", nameof(title));

        Title = title.Trim();
        Narrative = narrative?.Trim();
        Kpis = kpis?.ToList().AsReadOnly() ?? (IReadOnlyList<ReportKpiSummary>)Array.Empty<ReportKpiSummary>();
        TableHeaders = tableHeaders?.Select(h => h.Trim()).ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>();
        TableRows = tableRows?.Select(r => (IReadOnlyList<string>)r.Select(c => c ?? string.Empty).ToList().AsReadOnly()).ToList().AsReadOnly()
                    ?? (IReadOnlyList<IReadOnlyList<string>>)Array.Empty<IReadOnlyList<string>>();
    }

    public void UpdateNarrative(string? narrative)
    {
        Narrative = narrative?.Trim();
    }
}
