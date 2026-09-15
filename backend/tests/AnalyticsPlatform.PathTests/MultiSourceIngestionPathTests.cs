using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public class MultiSourceIngestionPathTests
{
    [Fact]
    public void RegisterMultipleSources_AllTypesCoexist()
    {
        var sources = new[]
        {
            new DataSourceDefinition("SalesQ1", DataSourceType.Excel, "./data/sales_q1.xlsx"),
            new DataSourceDefinition("SalesQ2", DataSourceType.Csv, "./data/sales_q2.csv"),
            new DataSourceDefinition("Customers", DataSourceType.SqlServer, "Server=.;Database=Crm;")
        };

        sources.Should().HaveCount(3);
        sources.Select(s => s.Type).Distinct().Should().HaveCount(3);
    }
}
