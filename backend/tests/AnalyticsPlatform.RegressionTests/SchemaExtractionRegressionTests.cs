using System.Text.Json;
using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;
using Xunit;

namespace AnalyticsPlatform.RegressionTests;

public class SchemaExtractionRegressionTests
{
    [Theory]
    [InlineData(new[] { "1", "42", "1000", "-5" }, ColumnDataType.Integer, false)]
    [InlineData(new[] { "3.14", "0.99", "1250.50" }, ColumnDataType.Decimal, false)]
    [InlineData(new[] { "10", "20.5", "30" }, ColumnDataType.Decimal, false)]
    [InlineData(new[] { "true", "False", "TRUE", "false" }, ColumnDataType.Boolean, false)]
    [InlineData(new[] { "2026-01-01", "2026-03-15T08:30:00Z" }, ColumnDataType.DateTime, false)]
    [InlineData(new[] { "Alpha", "Beta", "Gamma" }, ColumnDataType.String, false)]
    [InlineData(new[] { "100", "Unknown", "200" }, ColumnDataType.String, false)]
    [InlineData(new[] { "1", "", "3" }, ColumnDataType.Integer, true)]
    public void TypeInferenceEngine_ResolvesExpectedTypesPrecisely(string[] values, ColumnDataType expectedType, bool expectedNullable)
    {
        var result = TypeInferenceEngine.InferColumn(0, "TestCol", values);

        result.Schema.InferredType.Should().Be(expectedType);
        result.Schema.IsNullable.Should().Be(expectedNullable);
    }

    [Fact]
    public void TypeInferenceEngine_CapsSampleValuesAtFiveDistinctItems()
    {
        var tenValues = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

        var result = TypeInferenceEngine.InferColumn(0, "SamplesCol", tenValues);

        result.Schema.SampleValues.Should().HaveCount(5);
        result.Schema.SampleValues.Should().ContainInOrder("A", "B", "C", "D", "E");
    }

    [Fact]
    public void TypeInferenceEngine_AllNullOrEmpty_ResolvesUnknownAndNullable()
    {
        var nullValues = new object?[] { null, "", "   ", null };

        var result = TypeInferenceEngine.InferColumn(0, "EmptyCol", nullValues);

        result.Schema.InferredType.Should().Be(ColumnDataType.Unknown);
        result.Schema.IsNullable.Should().BeTrue();
        result.Schema.SampleValues.Should().BeEmpty();
    }

    [Fact]
    public async Task CsvSchemaExtractor_GoldenSnapshot_ProducesDeterministicContract()
    {
        var tempCsv = Path.Combine(Path.GetTempPath(), $"golden_csv_{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(tempCsv,
                "RecordId,MetricName,Score,Certified,LoggedAt\n" +
                "101,Accuracy,98.6,true,2026-05-01T09:00:00Z\n" +
                "102,Latency,12.4,false,2026-05-01T09:01:00Z\n");

            var reader = new CsvDataSourceReader();
            var schema = await reader.ExtractSchemaAsync(tempCsv);

            schema.Should().HaveCount(5);

            var json = JsonSerializer.Serialize(schema);

            json.Should().Contain("\"RecordId\"");
            json.Should().Contain("\"MetricName\"");
            json.Should().Contain("\"Score\"");
            json.Should().Contain("\"Certified\"");
            json.Should().Contain("\"LoggedAt\"");

            schema[0].InferredType.Should().Be(ColumnDataType.Integer);
            schema[1].InferredType.Should().Be(ColumnDataType.String);
            schema[2].InferredType.Should().Be(ColumnDataType.Decimal);
            schema[3].InferredType.Should().Be(ColumnDataType.Boolean);
            schema[4].InferredType.Should().Be(ColumnDataType.DateTime);
        }
        finally
        {
            if (File.Exists(tempCsv))
            {
                File.Delete(tempCsv);
            }
        }
    }
}

