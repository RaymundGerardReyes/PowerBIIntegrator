using ClosedXML.Excel;
using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Infrastructure.DataSourceConnectors;

public sealed class ExcelDataSourceReaderUnitTests : IDisposable
{
    private readonly string _tempXlsxPath;

    public ExcelDataSourceReaderUnitTests()
    {
        _tempXlsxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_test.xlsx");

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "PassengerId";
        ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "Age";
        ws.Cell(1, 4).Value = "Fare";
        ws.Cell(1, 5).Value = "Survived";

        ws.Cell(2, 1).Value = 1;
        ws.Cell(2, 2).Value = "Braund, Mr. Owen Harris";
        ws.Cell(2, 3).Value = 22;
        ws.Cell(2, 4).Value = 7.25;
        ws.Cell(2, 5).Value = false;

        ws.Cell(3, 1).Value = 2;
        ws.Cell(3, 2).Value = "Cumings, Mrs. John Bradley";
        ws.Cell(3, 3).Value = 38;
        ws.Cell(3, 4).Value = 71.2833;
        ws.Cell(3, 5).Value = true;

        workbook.SaveAs(_tempXlsxPath);
    }

    public void Dispose()
    {
        if (File.Exists(_tempXlsxPath))
        {
            try { File.Delete(_tempXlsxPath); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExtractSchemaAsync_ExtractsCorrectColumnsAndTypes()
    {
        var reader = new ExcelDataSourceReader();
        var schema = await reader.ExtractSchemaAsync(_tempXlsxPath);

        schema.Should().NotBeNull();
        schema.Should().HaveCount(5);

        schema[0].Name.Should().Be("PassengerId");
        schema[0].InferredType.Should().Be(ColumnDataType.Integer);

        schema[1].Name.Should().Be("Name");
        schema[1].InferredType.Should().Be(ColumnDataType.String);

        schema[2].Name.Should().Be("Age");
        schema[2].InferredType.Should().Be(ColumnDataType.Integer);

        schema[3].Name.Should().Be("Fare");
        schema[3].InferredType.Should().Be(ColumnDataType.Decimal);

        schema[4].Name.Should().Be("Survived");
        schema[4].InferredType.Should().Be(ColumnDataType.Boolean);
    }

    [Fact]
    public async Task ReadAsync_ReturnsAllRowsWithCorrectValues()
    {
        var reader = new ExcelDataSourceReader();
        var rows = await reader.ReadAsync(_tempXlsxPath);

        rows.Should().HaveCount(2);
        rows[0]["PassengerId"].Should().Be(1.0); // ExcelDataReader reads numeric as double
        rows[0]["Name"].Should().Be("Braund, Mr. Owen Harris");
        rows[1]["PassengerId"].Should().Be(2.0);
    }

    [Fact]
    public async Task ReadBatchesAsync_StreamsBatchesCorrectly()
    {
        var reader = new ExcelDataSourceReader();
        var batches = new List<IReadOnlyList<IDictionary<string, object?>>>();

        await foreach (var batch in reader.ReadBatchesAsync(_tempXlsxPath, batchSize: 1))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(2);
        batches[0].Should().HaveCount(1);
        batches[1].Should().HaveCount(1);
    }

    [Fact]
    public void ValidatePath_ThrowsOnTraversal()
    {
        var reader = new ExcelDataSourceReader();
        var act = () => reader.ReadAsync("../traversal.xlsx");

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*directory traversal*");
    }
}
