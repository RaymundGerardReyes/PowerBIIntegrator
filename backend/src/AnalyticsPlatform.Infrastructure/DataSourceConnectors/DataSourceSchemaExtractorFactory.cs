using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors;

public class DataSourceSchemaExtractorFactory : IDataSourceSchemaExtractorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataSourceSchemaExtractorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDataSourceSchemaExtractor GetExtractor(DataSourceType type)
    {
        var key = type switch
        {
            DataSourceType.Excel => "excel",
            DataSourceType.Csv => "csv",
            DataSourceType.SqlServer or DataSourceType.PostgreSql or DataSourceType.MySql => "sql",
            _ => throw new NotSupportedException($"Data source type '{type}' does not have a registered schema extractor.")
        };

        return _serviceProvider.GetRequiredKeyedService<IDataSourceSchemaExtractor>(key);
    }
}

