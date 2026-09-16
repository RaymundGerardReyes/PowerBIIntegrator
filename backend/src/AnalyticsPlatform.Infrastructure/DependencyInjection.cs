using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Domain.Repositories;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Excel;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Pdf;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Word;
using AnalyticsPlatform.Infrastructure.Persistence;
using AnalyticsPlatform.Infrastructure.PowerBi;
using AnalyticsPlatform.Infrastructure.Repositories;

namespace AnalyticsPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default") ?? "Server=localhost;Database=AnalyticsPlatform;Trusted_Connection=True;TrustServerCertificate=True;"));

        services.AddSingleton<IAnalyticsModelRepository, AnalyticsModelRepository>();
        services.AddSingleton<IDashboardRepository, DashboardRepository>();
        services.AddSingleton<IDataSourceRepository, DataSourceRepository>();

        services.AddScoped<IPowerBiPublisher, FabricRestClient>();
        services.AddScoped<IEmbedTokenService, EmbedTokenService>();
        
        services.AddScoped<PbirGenerator>();
        services.AddScoped<IPbirGenerator>(sp => sp.GetRequiredService<PbirGenerator>());
        
        services.AddScoped<TmdlGenerator>();
        services.AddScoped<ITmdlGenerator>(sp => sp.GetRequiredService<TmdlGenerator>());
        
        services.AddScoped<IPbipCompiler, PbipCompiler>();
        services.AddScoped<PbipPackager>();

        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceReader, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceReader, SqlServerConnector>("sql");

        services.AddKeyedScoped<IDataSourceSchemaExtractor, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, SqlServerConnector>("sql");

        services.AddScoped<IDataSourceSchemaExtractorFactory, DataSourceSchemaExtractorFactory>();

        // Document Generators (Multi-Target Rendering)
        services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();
        services.AddScoped<IExcelReportGenerator, ExcelReportGenerator>();
        services.AddScoped<IWordReportGenerator, WordReportGenerator>();

        return services;
    }
}
