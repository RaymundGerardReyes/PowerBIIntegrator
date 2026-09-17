using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;
using AnalyticsPlatform.Application.Features.Analytics.Queries.GetAnalyticsModels;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompileTmdlSemanticModel;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.LaunchLocalPowerBi;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.DownloadPbipPackage;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Queries.GetLocalPowerBiStatus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;
using AnalyticsPlatform.Infrastructure.PowerBi;
using AnalyticsPlatform.Infrastructure.Repositories;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public class DataOrchestrationSynthesisPathTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _csvPath;

    public DataOrchestrationSynthesisPathTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"orch_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _csvPath = Path.Combine(_tempDir, "titanic_survival.csv");
        File.WriteAllText(_csvPath,
            "PassengerId,Survived,Pclass,Name,Sex,Age,Fare\n" +
            "1,0,3,\"Braund, Mr. Owen Harris\",male,22,7.25\n" +
            "2,1,1,\"Cumings, Mrs. John Bradley (Florence Briggs Thayer)\",female,38,71.2833\n" +
            "3,1,3,\"Heikkinen, Miss. Laina\",female,26,7.925\n" +
            "4,1,1,\"Futrelle, Mrs. Jacques Heath (Lily May Peel)\",female,35,53.1000\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FullOrchestrationPipeline_FromIngestionToValidationTmdlAndPbip_SucceedsEndToEnd()
    {
        // 1. Arrange DI container with real production implementations
        var services = new ServiceCollection();
        services.AddSingleton<IDataSourceRepository, DataSourceRepository>();
        services.AddSingleton<IAnalyticsModelRepository, AnalyticsModelRepository>();
        services.AddSingleton<IDashboardRepository, DashboardRepository>();

        services.AddKeyedScoped<IDataSourceReader, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceReader, SqlServerConnector>("sql");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, SqlServerConnector>("sql");
        services.AddScoped<IDataSourceSchemaExtractorFactory, DataSourceSchemaExtractorFactory>();

        services.AddSingleton<ITmdlGenerator, TmdlGenerator>();
        services.AddSingleton<IPbirGenerator, PbirGenerator>();
        services.AddSingleton<IPbipCompiler, PbipCompiler>();

        var sp = services.BuildServiceProvider();

        var dataSourceRepo = sp.GetRequiredService<IDataSourceRepository>();
        var modelRepo = sp.GetRequiredService<IAnalyticsModelRepository>();
        var dashboardRepo = sp.GetRequiredService<IDashboardRepository>();
        var extractorFactory = sp.GetRequiredService<IDataSourceSchemaExtractorFactory>();
        var tmdlGenerator = sp.GetRequiredService<ITmdlGenerator>();
        var pbirGenerator = sp.GetRequiredService<IPbirGenerator>();
        var pbipCompiler = sp.GetRequiredService<IPbipCompiler>();

        // 2. Step 1: Ingest / Register Data Source -> Automatically synthesizes AnalyticsModel
        var registerHandler = new RegisterDataSourceCommandHandler(dataSourceRepo, extractorFactory, modelRepo);
        var registerCmd = new RegisterDataSourceCommand("TitanicSurvival2026", DataSourceType.Csv, _csvPath);
        var registerResult = await registerHandler.Handle(registerCmd, CancellationToken.None);

        registerResult.IsSuccess.Should().BeTrue();
        registerResult.Value.Should().NotBeNull();
        registerResult.Value!.Name.Should().Be("TitanicSurvival2026");
        registerResult.Value.Schema.Should().HaveCount(7);

        // 3. Step 2: Query Semantic Models -> Returns synthesized model with columns & measures
        var getModelsHandler = new GetAnalyticsModelsQueryHandler(modelRepo, dataSourceRepo);
        var modelsResult = await getModelsHandler.Handle(new GetAnalyticsModelsQuery(), CancellationToken.None);

        modelsResult.IsSuccess.Should().BeTrue();
        var models = modelsResult.Value;
        models.Should().NotBeNull();
        models.Should().NotBeEmpty();

        var titanicModel = models!.FirstOrDefault(m => m.Name.Contains("TitanicSurvival2026", StringComparison.OrdinalIgnoreCase));
        titanicModel.Should().NotBeNull("Semantic model must be synthesized for registered Titanic data source");
        titanicModel!.Id.Should().NotBeEmpty();
        titanicModel.Tables.Should().Contain("TitanicSurvival2026");
        titanicModel.Measures.Should().Contain("TotalRows");

        // 4. Step 3: Validate Model via Domain Validation Rules Engine
        var validateHandler = new ValidateAnalyticsModelCommandHandler(modelRepo);
        var validateResult = await validateHandler.Handle(new ValidateAnalyticsModelCommand(titanicModel.Id), CancellationToken.None);

        validateResult.IsSuccess.Should().BeTrue();
        validateResult.Value.Should().NotBeNull();
        validateResult.Value!.IsValid.Should().BeTrue("Synthesized model should be mathematically and structurally valid");
        validateResult.Value.Errors.Should().BeEmpty();
        validateResult.Value.DetectedCycles.Should().BeEmpty();

        // 5. Step 4: Compile TMDL Semantic Model Definition
        var compileTmdlHandler = new CompileTmdlSemanticModelCommandHandler(tmdlGenerator, modelRepo);
        var tmdlResult = await compileTmdlHandler.Handle(new CompileTmdlSemanticModelCommand(titanicModel.Id), CancellationToken.None);

        tmdlResult.IsSuccess.Should().BeTrue();
        tmdlResult.Value.Should().NotBeNull();
        tmdlResult.Value!.TotalFiles.Should().BeGreaterThan(0);
        tmdlResult.Value.Files.Keys.Should().Contain(k => k.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase));

        // Verify TMDL content contains table schema and COUNTROWS measure
        var tableFile = tmdlResult.Value.Files.FirstOrDefault(kv => kv.Key.Contains("TitanicSurvival2026"));
        tableFile.Value.Should().NotBeNullOrWhiteSpace();
        tableFile.Value.Should().Contain("table 'TitanicSurvival2026'");
        tableFile.Value.Should().Contain("column 'PassengerId'");
        tableFile.Value.Should().Contain("column 'Name'");
        tableFile.Value.Should().Contain("measure 'TotalRows' = COUNTROWS('TitanicSurvival2026')");

        // 5.5 Step 4.5: Compile PBIR Report Definition tailored to the dataset
        var compilePbirHandler = new CompilePbirDefinitionCommandHandler(pbirGenerator, dashboardRepo, modelRepo);
        var pbirResult = await compilePbirHandler.Handle(new CompilePbirDefinitionCommand(Guid.NewGuid(), "../definition", titanicModel.Id), CancellationToken.None);
        pbirResult.IsSuccess.Should().BeTrue();
        pbirResult.Value.Should().NotBeNull();
        pbirResult.Value!.TotalFiles.Should().BeGreaterThan(0);
        var visualFiles = pbirResult.Value.Files.Where(f => f.Key.Contains("visual.json")).Select(f => f.Value).ToList();
        visualFiles.Should().NotBeEmpty();
        visualFiles.Should().Contain(v => v.Contains("TitanicSurvival2026"));

        // 6. Step 5: Download PBIP Package Archive (.zip containing .pbip + .pbir + .tmdl)
        var downloadPbipHandler = new DownloadPbipPackageQueryHandler(pbipCompiler, dashboardRepo, modelRepo);
        var downloadQuery = new DownloadPbipPackageQuery(Guid.NewGuid(), titanicModel.Id, "TitanicAnalyticsProject");
        var downloadResult = await downloadPbipHandler.Handle(downloadQuery, CancellationToken.None);

        downloadResult.IsSuccess.Should().BeTrue();
        downloadResult.Value.Should().NotBeNull();
        downloadResult.Value!.ContentType.Should().Be("application/zip");
        downloadResult.Value.FileName.Should().Be("TitanicAnalyticsProject.pbip.zip");
        downloadResult.Value.ZipBytes.Should().NotBeEmpty();

        // Inspect zip archive entries in memory
        using var zipStream = new MemoryStream(downloadResult.Value.ZipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        archive.Entries.Should().Contain(e => e.FullName == "TitanicAnalyticsProject.pbip");
        archive.Entries.Should().Contain(e => e.FullName.Contains("definition/version.json"));
        archive.Entries.Should().Contain(e => e.FullName.Contains("definition/report.json"));
        archive.Entries.Should().Contain(e => e.FullName.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase));

        // 7. Step 6: Test Local Power BI Desktop Orchestration (Status & PBIP generation to disk)
        var desktopService = new LocalPowerBiDesktopService(
            new PbipPackager(),
            new ConfigurationBuilder().Build(),
            NullLogger<LocalPowerBiDesktopService>.Instance);

        var statusHandler = new GetLocalPowerBiStatusQueryHandler(desktopService);
        var status = await statusHandler.Handle(new GetLocalPowerBiStatusQuery(), CancellationToken.None);
        status.Should().NotBeNull();
        status.IsInstalled.Should().BeTrue("Power BI Desktop is installed on this Windows 11 host");

        var testOutputDir = Path.Combine(Path.GetTempPath(), "test_pbip_output_" + Guid.NewGuid().ToString("N"));
        try
        {
            var launchHandler = new LaunchLocalPowerBiCommandHandler(desktopService, pbirGenerator, tmdlGenerator, dashboardRepo, modelRepo);
            var launchCmd = new LaunchLocalPowerBiCommand(Guid.NewGuid(), titanicModel.Id, "TitanicLocalDesktopProject", testOutputDir);
            var launchResult = await launchHandler.Handle(launchCmd, CancellationToken.None);

            launchResult.IsSuccess.Should().BeTrue();
            launchResult.Value.Should().NotBeNull();
            launchResult.Value!.PbipFilePath.Should().EndWith("TitanicLocalDesktopProject.pbip");
            File.Exists(launchResult.Value.PbipFilePath).Should().BeTrue("Local PBIP file must be written to disk");
        }
        finally
        {
            if (Directory.Exists(testOutputDir))
            {
                Directory.Delete(testOutputDir, true);
            }
        }
    }
}
