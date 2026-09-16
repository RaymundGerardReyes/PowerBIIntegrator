using FluentAssertions;
using Xunit;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;

namespace AnalyticsPlatform.UnitTests.Infrastructure.DataSourceConnectors;

public class PostgresConnectorUnitTests
{
    [Theory]
    [InlineData("boolean", ColumnDataType.Boolean)]
    [InlineData("bool", ColumnDataType.Boolean)]
    [InlineData("smallint", ColumnDataType.Integer)]
    [InlineData("integer", ColumnDataType.Integer)]
    [InlineData("bigint", ColumnDataType.Integer)]
    [InlineData("serial", ColumnDataType.Integer)]
    [InlineData("real", ColumnDataType.Decimal)]
    [InlineData("double precision", ColumnDataType.Decimal)]
    [InlineData("numeric", ColumnDataType.Decimal)]
    [InlineData("money", ColumnDataType.Decimal)]
    [InlineData("date", ColumnDataType.DateTime)]
    [InlineData("timestamp", ColumnDataType.DateTime)]
    [InlineData("timestamptz", ColumnDataType.DateTime)]
    [InlineData("text", ColumnDataType.String)]
    [InlineData("varchar", ColumnDataType.String)]
    public void MapPostgresTypeNameToColumnDataType_MapsCorrectly(string pgType, ColumnDataType expected)
    {
        var result = PostgresConnector.MapPostgresTypeNameToColumnDataType(pgType);
        result.Should().Be(expected);
    }

    [Fact]
    public void MapSystemTypeToColumnDataType_MapsStandardTypes()
    {
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(int)).Should().Be(ColumnDataType.Integer);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(long)).Should().Be(ColumnDataType.Integer);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(double)).Should().Be(ColumnDataType.Decimal);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(decimal)).Should().Be(ColumnDataType.Decimal);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(DateTime)).Should().Be(ColumnDataType.DateTime);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(bool)).Should().Be(ColumnDataType.Boolean);
        PostgresConnector.MapSystemTypeToColumnDataType(typeof(string)).Should().Be(ColumnDataType.String);
    }
}
