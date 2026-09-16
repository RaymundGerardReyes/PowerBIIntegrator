using AnalyticsPlatform.Domain.Common;

namespace AnalyticsPlatform.Domain.Features.DataQuality.Entities;

public class DatasetProfile : Entity
{
    public string DatasetName { get; private set; }
    public string SourceReference { get; private set; }
    public long TotalRows { get; private set; }
    public DateTime ProfiledAtUtc { get; private set; }
    public List<ColumnProfile> ColumnProfiles { get; } = new();

    public DatasetProfile(string datasetName, string sourceReference, long totalRows)
    {
        if (string.IsNullOrWhiteSpace(datasetName))
            throw new DomainException("Dataset name cannot be empty.");

        DatasetName = datasetName;
        SourceReference = sourceReference;
        TotalRows = totalRows;
        ProfiledAtUtc = DateTime.UtcNow;
    }

    public void AddColumnProfile(ColumnProfile columnProfile)
    {
        ColumnProfiles.Add(columnProfile);
    }
}
