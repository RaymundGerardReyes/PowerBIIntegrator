namespace AnalyticsPlatform.Domain.Features.ReportGeneration.ValueObjects;

public sealed record ReportMetadata
{
    public string Title { get; init; }
    public string? Subtitle { get; init; }
    public string Author { get; init; }
    public string Organization { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string Version { get; init; }

    public ReportMetadata(
        string title,
        string? subtitle = null,
        string author = "System",
        string organization = "Enterprise Analytics Platform",
        DateTimeOffset? generatedAt = null,
        string version = "1.0.0")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Report title cannot be empty or whitespace.", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("Report title cannot exceed 200 characters.", nameof(title));

        Title = title.Trim();
        Subtitle = subtitle?.Trim();
        Author = string.IsNullOrWhiteSpace(author) ? "System" : author.Trim();
        Organization = string.IsNullOrWhiteSpace(organization) ? "Enterprise Analytics Platform" : organization.Trim();
        GeneratedAt = generatedAt ?? DateTimeOffset.UtcNow;
        Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim();
    }
}
