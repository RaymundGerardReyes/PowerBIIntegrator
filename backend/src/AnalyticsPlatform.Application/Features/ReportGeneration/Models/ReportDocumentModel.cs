namespace AnalyticsPlatform.Application.Features.ReportGeneration.Models;

public sealed record ReportKpiDto(
    string Label,
    string Value,
    string? DeltaPercentage = null,
    string? Description = null);

public sealed record ReportSectionDto(
    string Title,
    string? Narrative = null,
    IReadOnlyList<ReportKpiDto>? Kpis = null,
    IReadOnlyList<string>? TableHeaders = null,
    IReadOnlyList<IReadOnlyList<string>>? TableRows = null);

public sealed record ReportDocumentModel(
    string Title,
    string? Subtitle = null,
    string Author = "System",
    string Organization = "Enterprise Analytics Platform",
    DateTimeOffset? GeneratedAt = null,
    IReadOnlyList<ReportSectionDto>? Sections = null);

public sealed record GeneratedReportDto(
    byte[] Content,
    string ContentType,
    string FileName);
