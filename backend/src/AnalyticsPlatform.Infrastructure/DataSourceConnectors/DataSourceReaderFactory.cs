using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;

namespace AnalyticsPlatform.Infrastructure.DataSourceConnectors;

public class DataSourceReaderFactory : IDataSourceReaderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataSourceReaderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDataSourceReader GetReader(DataSourceType type)
    {
        var key = type switch
        {
            DataSourceType.Excel => "excel",
            DataSourceType.Csv => "csv",
            DataSourceType.SqlServer or DataSourceType.MySql => "sql",
            DataSourceType.PostgreSql => "postgres",
            _ => throw new NotSupportedException($"Data source type '{type}' does not have a registered reader.")
        };

        return _serviceProvider.GetRequiredKeyedService<IDataSourceReader>(key);
    }
}
