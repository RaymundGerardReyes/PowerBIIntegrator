using FluentAssertions;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using AnalyticsPlatform.Domain.Features.DataQuality.Rules;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.DataQuality;

public class SchemaCompatibilityRuleTests
{
    [Fact]
    public void Evaluate_WhenColumnsMatch_ReturnsNoViolations()
    {
        var contract = new SchemaContract("Sales");
        contract.AddColumn(new ColumnContract("Id", "Int64", false, true));
        contract.AddColumn(new ColumnContract("Amount", "Decimal", false, false));

        var profile = new DatasetProfile("SalesBatch", "source.csv", 10);
        profile.AddColumnProfile(new ColumnProfile("Id", "Int64", 10, 0, 0.0, 10, "1", "10", new[] { "1" }, @"^\d+$", "High"));
        profile.AddColumnProfile(new ColumnProfile("Amount", "Decimal", 10, 0, 0.0, 8, "10", "100", new[] { "10" }, @"^\d+$", "High"));

        var result = SchemaCompatibilityRule.Evaluate(contract, profile);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_WhenRequiredColumnMissing_ReturnsFatalViolation()
    {
        var contract = new SchemaContract("Sales");
        contract.AddColumn(new ColumnContract("RequiredCol", "String", false, false));

        var profile = new DatasetProfile("SalesBatch", "source.csv", 10);

        var result = SchemaCompatibilityRule.Evaluate(contract, profile);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(v => v.Column == "RequiredCol" && v.IsFatal);
    }
}
