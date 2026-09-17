using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IDataSourceReaderFactory
{
    IDataSourceReader GetReader(DataSourceType type);
}
