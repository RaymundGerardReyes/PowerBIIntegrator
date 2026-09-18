using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Infrastructure.PowerBi;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.PowerBi;

public class TmdlGeneratorTests
{
    [Fact]
    public void GenerateSemanticModel_WhenPartitionIsEmpty_GeneratesValidTypedMTablePartition()
    {
        // Arrange
        var generator = new TmdlGenerator();
        var model = new AnalyticsModel("TestSalesModel");
        var table = new ModelTable("Sales");
        table.AddColumn(new ModelColumn("Revenue", ColumnDataType.Decimal, "Revenue"));
        table.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        table.AddColumn(new ModelColumn("OrderDate", ColumnDataType.DateTime, "OrderDate"));
        table.AddMeasure(Measure.Create("TotalRevenue", new MeasureExpression("SUM('Sales'[Revenue])", "Decimal"), "Sales").Value!);
        table.AddMeasure(Measure.Create("TotalRows", new MeasureExpression("COUNTROWS('Sales')", "Int64"), "Sales").Value!);
        model.AddTable(table);

        // Act
        var fileTree = generator.GenerateSemanticModel(model);
        var tableFile = fileTree.Files.FirstOrDefault(f => f.RelativePath.Contains("Sales.tmdl"));

        // Assert
        tableFile.Should().NotBeNull();
        var tmdl = tableFile!.Content;
        tmdl.Should().Contain("table 'Sales'");
        tmdl.Should().Contain("column 'Revenue'");
        tmdl.Should().Contain("column 'Region'");
        tmdl.Should().Contain("measure 'TotalRevenue' = SUM('Sales'[Revenue])");
        tmdl.Should().Contain("measure 'TotalRows' = COUNTROWS('Sales')");
        // Must NOT contain empty table schema
        tmdl.Should().NotContain("#table(type table [], {})");
        // Must contain typed table definition with sample rows
        tmdl.Should().Contain("#table(type table [");
        tmdl.Should().Contain("#\"Revenue\" = Double.Type");
        tmdl.Should().Contain("#\"Region\" = Text.Type");
        tmdl.Should().Contain("#\"OrderDate\" = DateTime.Type");
    }

    [Fact]
    public void GenerateSemanticModel_WhenPartitionIsProvided_PreservesCustomPartition()
    {
        // Arrange
        var generator = new TmdlGenerator();
        var model = new AnalyticsModel("TitanicModel");
        var table = new ModelTable("titanic");
        table.AddColumn(new ModelColumn("target", ColumnDataType.Int64, "target"));
        table.AddMeasure(Measure.Create("target_Rate", new MeasureExpression("AVERAGE('titanic'[target])", "Double"), "titanic").Value!);
        table.AddMeasure(Measure.Create("TotalRows", new MeasureExpression("COUNTROWS('titanic')", "Int64"), "titanic").Value!);
        table.SetMQueryPartition("titanic-Partition", "let Source = Csv.Document(File.Contents(\"C:/data/titanic.csv\")) in Source");
        model.AddTable(table);

        // Act
        var fileTree = generator.GenerateSemanticModel(model);
        var tableFile = fileTree.Files.FirstOrDefault(f => f.RelativePath.Contains("titanic.tmdl"));

        // Assert
        tableFile.Should().NotBeNull();
        var tmdl = tableFile!.Content;
        tmdl.Should().Contain("table 'titanic'");
        tmdl.Should().Contain("measure 'target_Rate' = AVERAGE('titanic'[target])");
        tmdl.Should().Contain("File.Contents(\"C:/data/titanic.csv\")");
    }

    [Fact]
    public void CreateFromDataSource_WhenExcelSource_GeneratesExcelWorkbookPartitionWithTypeNumber()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_bike_sales_{Guid.NewGuid():N}.xlsx");
        File.WriteAllText(tempFile, "dummy excel content");

        try
        {
            var schema = new List<AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnSchema>
            {
                new(0, "Product", AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType.String, false, Array.Empty<string>()),
                new(1, "Price", AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType.Decimal, false, Array.Empty<string>()),
                new(2, "UnitsSold", AnalyticsPlatform.Domain.Features.DataSources.Entities.ColumnDataType.Integer, false, Array.Empty<string>())
            };

            // Act
            var model = AnalyticsPlatform.Application.Features.Analytics.Services.AnalyticsModelFactory.CreateFromDataSource(
                "Bike_Sales_2021.xlsx",
                schema,
                connectionOrPath: tempFile,
                sourceType: AnalyticsPlatform.Domain.Features.DataSources.Entities.DataSourceType.Excel);

            var table = model.Tables.First();
            var partition = table.MQueryPartition;

            // Assert
            partition.Should().NotBeNull();
            partition.Should().Contain("Excel.Workbook(File.Contents(");
            partition.Should().Contain("Table.TransformColumnTypes");
            partition.Should().Contain("{\"Price\", type number}");
            partition.Should().NotContain("{\"Price\", number}");
            partition.Should().Contain("{\"UnitsSold\", Int64.Type}");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}

