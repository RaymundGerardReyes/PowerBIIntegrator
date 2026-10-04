using FluentAssertions;
using AnalyticsPlatform.Application.Features.Analytics.Services;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.DataQuality;

public class AnalyticsModelFactoryMQueryTests
{
    [Fact]
    public void CreateFromDataSource_Csv_GeneratesAppliedStepsWithCleanedText()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_csv_{Guid.NewGuid()}.csv");
        try
        {
            File.WriteAllText(tempFile, "EmpId,Name,Cost\n001,John,500\n");

            var schema = new List<ColumnSchema>
            {
                new(0, "EmpId", ColumnDataType.String, false, Array.Empty<string>()),
                new(1, "Name", ColumnDataType.String, false, Array.Empty<string>()),
                new(2, "Cost", ColumnDataType.Decimal, false, Array.Empty<string>())
            };

            var model = AnalyticsModelFactory.CreateFromDataSource(
                "Employees.csv",
                schema,
                connectionOrPath: tempFile,
                sourceType: DataSourceType.Csv);

            var table = model.Tables.First();
            var partition = table.MQueryPartition;

            partition.Should().NotBeNull();
            partition.Should().Contain("Source = Csv.Document(File.Contents(");
            partition.Should().Contain("#\"Promoted Headers\" = Table.PromoteHeaders(Source, [PromoteAllScalars=true])");
            partition.Should().Contain("#\"Changed Type\" = Table.TransformColumnTypes(#\"Promoted Headers\",");
            partition.Should().Contain("#\"Cleaned Text\" = Table.TransformColumns(#\"Changed Type\",");
            partition.Should().Contain("{\"Name\", Text.Trim, type text}");
            partition.Should().Contain("in\n    #\"Cleaned Text\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void CreateFromDataSource_Excel_GeneratesDataSheetAndCleanedText()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid()}.xlsx");
        try
        {
            File.WriteAllText(tempFile, "mock excel binary");

            var schema = new List<ColumnSchema>
            {
                new(0, "Product", ColumnDataType.String, false, Array.Empty<string>()),
                new(1, "Price", ColumnDataType.Decimal, false, Array.Empty<string>())
            };

            var model = AnalyticsModelFactory.CreateFromDataSource(
                "Sales.xlsx",
                schema,
                connectionOrPath: tempFile,
                sourceType: DataSourceType.Excel);

            var table = model.Tables.First();
            var partition = table.MQueryPartition;

            partition.Should().NotBeNull();
            partition.Should().Contain("Source = Excel.Workbook(File.Contents(");
            partition.Should().Contain("DataSheet = Source{0}[Data]");
            partition.Should().Contain("#\"Promoted Headers\" = Table.PromoteHeaders(DataSheet, [PromoteAllScalars=true])");
            partition.Should().Contain("#\"Changed Type\" = Table.TransformColumnTypes(#\"Promoted Headers\",");
            partition.Should().Contain("#\"Cleaned Text\" = Table.TransformColumns(#\"Changed Type\",");
            partition.Should().Contain("{\"Product\", Text.Trim, type text}");
            partition.Should().Contain("in\n    #\"Cleaned Text\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
