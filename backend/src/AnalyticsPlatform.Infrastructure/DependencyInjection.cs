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
        services.AddScoped<ILocalPowerBiDesktopService, LocalPowerBiDesktopService>();

        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceReader, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceReader, SqlServerConnector>("sql");
        services.AddKeyedScoped<IDataSourceReader, PostgresConnector>("postgres");
        services.AddKeyedScoped<IDataSourceReader, PostgresConnector>("postgresql");

        services.AddKeyedScoped<IDataSourceSchemaExtractor, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, SqlServerConnector>("sql");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, PostgresConnector>("postgres");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, PostgresConnector>("postgresql");

        services.AddScoped<IDataSourceSchemaExtractorFactory, DataSourceSchemaExtractorFactory>();
        services.AddScoped<IDataSourceReaderFactory, DataSourceReaderFactory>();

        // Document Generators (Multi-Target Rendering)
        services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();
        services.AddScoped<IExcelReportGenerator, ExcelReportGenerator>();
        services.AddScoped<IWordReportGenerator, WordReportGenerator>();

        // LLM / MCP Orchestration Infrastructure
        services.AddSingleton<Application.Features.LlmOrchestration.Contracts.ILlmPolicyRepository, Llm.Policy.LlmPolicyRepository>();
        services.AddSingleton<Application.Features.LlmOrchestration.Contracts.IPromptGuardrailService, Llm.Guardrails.PiiRedactionService>();
        services.AddSingleton<Application.Features.LlmOrchestration.Contracts.IResponseGuardrailService, Llm.Guardrails.OutputContentFilter>();
        
        services.AddHttpClient<Application.Features.LlmOrchestration.Contracts.IOllamaClient, Llm.Providers.OllamaLocalClient>(client =>
        {
            var ollamaUrl = configuration["Llm:OllamaBaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<Llm.Providers.OpenAiCloudClient>();
        services.AddTransient<Application.Features.LlmOrchestration.Contracts.ICloudLlmClient>(sp => sp.GetRequiredService<Llm.Providers.OpenAiCloudClient>());

        services.AddHttpClient<Llm.Providers.AnthropicCloudClient>();
        services.AddTransient<Application.Features.LlmOrchestration.Contracts.ICloudLlmClient>(sp => sp.GetRequiredService<Llm.Providers.AnthropicCloudClient>());

        services.AddScoped<Application.Features.LlmOrchestration.Contracts.ILlmGateway, Llm.Policy.ProviderRouter>();

        // Data Quality & Transformation Engine (DQTE) Infrastructure Adapters
        services.AddScoped<Features.DataQuality.Connectors.TabularBatchReader>();
        services.AddScoped<Features.DataQuality.Dedupe.HashDedupeEngine>();
        services.AddScoped<Features.DataQuality.Dedupe.CompositeKeyDedupeEngine>();
        services.AddScoped<Features.DataQuality.Dedupe.SimilarityClusterDedupeEngine>();
        services.AddSingleton<Features.DataQuality.Repositories.SchemaContractRepository>();
        services.AddSingleton<Features.DataQuality.Repositories.PipelineRunRepository>();
        services.AddSingleton<Features.DataQuality.Persistence.QuarantineWriter>();

        // AI Advisory Tier Infrastructure Adapters & Tools
        services.AddSingleton<Application.Features.AiAdvisory.Interfaces.IAdvisoryRunRepository, Features.AiAdvisory.Repositories.InMemoryAdvisoryRunRepository>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetPipelineRunResultTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetDatasetProfileTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetDuplicateClustersTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetSchemaViolationsTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetTransformationPlanTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryTool, Features.AiAdvisory.Tools.GetChartSuggestionsTool>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryToolRegistry, Features.AiAdvisory.ToolRegistry.AdvisoryToolRegistry>();
        services.AddScoped<Application.Features.AiAdvisory.Services.AdvisoryContextAssembler>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IGroundedAdvisorySynthesizer, Features.AiAdvisory.Synthesizer.GroundedAdvisorySynthesizer>();
        services.AddScoped<Application.Features.AiAdvisory.Interfaces.IAdvisoryAuditLogger, Features.AiAdvisory.Audit.AdvisoryAuditLogger>();

        return services;
    }
}
