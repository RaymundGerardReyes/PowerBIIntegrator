using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;
using AnalyticsPlatform.Infrastructure.Repositories;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public class DataSourceRegistrationPathTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _csvPath;
    private readonly string _excelPath;

    public DataSourceRegistrationPathTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"path_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _csvPath = Path.Combine(_tempDir, "transactions.csv");
        File.WriteAllText(_csvPath,
            "TransactionId,Customer,Amount,IsSettled,Timestamp\n" +
            "1001,Alpha Retail,250.75,true,2026-03-01T10:00:00Z\n" +
            "1002,Beta Logistics,1420.00,false,2026-03-02T11:30:00Z\n" +
            "1003,Gamma Supply,89.90,true,2026-03-03T15:45:00Z\n");

        _excelPath = Path.Combine(_tempDir, "inventory.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Stock");
        ws.Cell(1, 1).Value = "Sku";
        ws.Cell(1, 2).Value = "Quantity";
        ws.Cell(1, 3).Value = "UnitPrice";
        ws.Cell(1, 4).Value = "Available";

        ws.Cell(2, 1).Value = "SKU-001";
        ws.Cell(2, 2).Value = 150;
        ws.Cell(2, 3).Value = 12.50;
        ws.Cell(2, 4).Value = true;

        ws.Cell(3, 1).Value = "SKU-002";
        ws.Cell(3, 2).Value = 300;
        ws.Cell(3, 3).Value = 4.75;
        ws.Cell(3, 4).Value = true;

        workbook.SaveAs(_excelPath);
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
    public async Task FullCsvRegistrationPipeline_ExtractsSchemaAndStoresInRepository_EndToEnd()
    {
        // 1. Build Service Provider with clean DI
        var services = new ServiceCollection();
        services.AddSingleton<IDataSourceRepository, DataSourceRepository>();
        services.AddKeyedScoped<IDataSourceReader, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceReader, SqlServerConnector>("sql");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, CsvDataSourceReader>("csv");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, SqlServerConnector>("sql");
        services.AddScoped<IDataSourceSchemaExtractorFactory, DataSourceSchemaExtractorFactory>();
        var sp = services.BuildServiceProvider();

        var repo = sp.GetRequiredService<IDataSourceRepository>();
        var factory = sp.GetRequiredService<IDataSourceSchemaExtractorFactory>();
        var handler = new RegisterDataSourceCommandHandler(repo, factory);

        // 2. Act - Execute Command
        var command = new RegisterDataSourceCommand("TransactionsFeed", DataSourceType.Csv, _csvPath);
        var result = await handler.Handle(command, CancellationToken.None);

        // 3. Assert Response
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TransactionsFeed");
        result.Value.Type.Should().Be("csv");

        var schema = result.Value.Schema;
        schema.Should().HaveCount(5);

        // Verify ordinal sequence
        for (int i = 0; i < schema.Count; i++)
        {
            schema[i].Ordinal.Should().Be(i);
        }

        // Verify column types and samples
        schema[0].Name.Should().Be("TransactionId");
        schema[0].InferredType.Should().Be(ColumnDataType.Integer);
        schema[0].SampleValues.Should().Contain("1001");

        schema[1].Name.Should().Be("Customer");
        schema[1].InferredType.Should().Be(ColumnDataType.String);
        schema[1].SampleValues.Should().Contain("Alpha Retail");

        schema[2].Name.Should().Be("Amount");
        schema[2].InferredType.Should().Be(ColumnDataType.Decimal);

        schema[3].Name.Should().Be("IsSettled");
        schema[3].InferredType.Should().Be(ColumnDataType.Boolean);

        schema[4].Name.Should().Be("Timestamp");
        schema[4].InferredType.Should().Be(ColumnDataType.DateTime);

        // 4. Verify Repository Retrieval
        var stored = await repo.GetByIdAsync(result.Value.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("TransactionsFeed");
        stored.Schema.Should().HaveCount(5);
    }

    [Fact]
    public async Task FullExcelRegistrationAndStreamingPipeline_ExtractsSchemaAndStreamsBatches()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDataSourceRepository, DataSourceRepository>();
        services.AddKeyedScoped<IDataSourceReader, ExcelDataSourceReader>("excel");
        services.AddKeyedScoped<IDataSourceSchemaExtractor, ExcelDataSourceReader>("excel");
        services.AddScoped<IDataSourceSchemaExtractorFactory, DataSourceSchemaExtractorFactory>();
        var sp = services.BuildServiceProvider();

        var repo = sp.GetRequiredService<IDataSourceRepository>();
        var factory = sp.GetRequiredService<IDataSourceSchemaExtractorFactory>();
        var handler = new RegisterDataSourceCommandHandler(repo, factory);

        var command = new RegisterDataSourceCommand("InventoryReport", DataSourceType.Excel, _excelPath);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Schema.Should().HaveCount(4);
        result.Value.Schema[0].Name.Should().Be("Sku");
        result.Value.Schema[0].InferredType.Should().Be(ColumnDataType.String);

        result.Value.Schema[1].Name.Should().Be("Quantity");
        result.Value.Schema[1].InferredType.Should().Be(ColumnDataType.Integer);

        result.Value.Schema[2].Name.Should().Be("UnitPrice");
        result.Value.Schema[2].InferredType.Should().Be(ColumnDataType.Decimal);

        result.Value.Schema[3].Name.Should().Be("Available");
        result.Value.Schema[3].InferredType.Should().Be(ColumnDataType.Boolean);

        // Verify streaming chunked reader
        var reader = sp.GetRequiredKeyedService<IDataSourceReader>("excel");
        var batches = new List<IReadOnlyList<IDictionary<string, object?>>>();
        await foreach (var batch in reader.ReadBatchesAsync(_excelPath, batchSize: 1))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(2); // 2 data rows with batchSize: 1 yields 2 batches
        batches[0][0]["Sku"].Should().Be("SKU-001");
        batches[1][0]["Sku"].Should().Be("SKU-002");
    }
}

