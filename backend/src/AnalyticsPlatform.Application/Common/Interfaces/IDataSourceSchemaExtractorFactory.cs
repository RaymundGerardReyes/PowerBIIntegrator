using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Application.Common.Interfaces;

public interface IDataSourceSchemaExtractorFactory
{
    IDataSourceSchemaExtractor GetExtractor(DataSourceType type);
}

