using FluentAssertions;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Csv;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Excel;
using AnalyticsPlatform.Infrastructure.DataSourceConnectors.Sql;
using Xunit;

namespace AnalyticsPlatform.SecurityTests;

public class DataSourcePathTraversalSecurityTests
{
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32\\drivers\\etc\\hosts")]
    [InlineData("data/../../secret.csv")]
    public async Task CsvDataSourceReader_WhenPathContainsTraversal_ThrowsInvalidOperationException(string maliciousPath)
    {
        var reader = new CsvDataSourceReader();

        var actRead = () => reader.ReadAsync(maliciousPath);
        await actRead.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*traversal*");

        var actExtract = () => reader.ExtractSchemaAsync(maliciousPath);
        await actExtract.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*traversal*");
    }

    [Theory]
    [InlineData("../../../financial_secrets.xlsx")]
    [InlineData("..\\confidential.xlsx")]
    [InlineData("uploads/../../internal.xlsx")]
    public async Task ExcelDataSourceReader_WhenPathContainsTraversal_ThrowsInvalidOperationException(string maliciousPath)
    {
        var reader = new ExcelDataSourceReader();

        var actRead = () => reader.ReadAsync(maliciousPath);
        await actRead.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*traversal*");

        var actExtract = () => reader.ExtractSchemaAsync(maliciousPath);
        await actExtract.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*traversal*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SqlServerConnector_WhenConnectionStringIsEmpty_ThrowsArgumentNullException(string? invalidConn)
    {
        var connector = new SqlServerConnector();

        var actRead = () => connector.ReadAsync(invalidConn!);
        await actRead.Should().ThrowAsync<ArgumentNullException>();

        var actExtract = () => connector.ExtractSchemaAsync(invalidConn!);
        await actExtract.Should().ThrowAsync<ArgumentNullException>();
    }
}

