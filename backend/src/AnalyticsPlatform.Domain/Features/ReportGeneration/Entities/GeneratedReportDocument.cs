using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.ReportGeneration.Entities;

public sealed class GeneratedReportDocument : Entity
{
    public byte[] ContentBytes { get; private set; }
    public string ContentType { get; private set; }
    public string FileName { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public GeneratedReportDocument(
        byte[] contentBytes,
        string contentType,
        string fileName,
        DateTimeOffset? generatedAt = null)
    {
        if (contentBytes == null || contentBytes.Length == 0)
            throw new ArgumentException("Report content cannot be empty.", nameof(contentBytes));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        ContentBytes = contentBytes;
        ContentType = contentType.Trim();
        FileName = fileName.Trim();
        GeneratedAt = generatedAt ?? DateTimeOffset.UtcNow;
    }
}
