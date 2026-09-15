using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Analytics.Rules;
using Xunit;

namespace AnalyticsPlatform.SecurityTests;

public class InputValidationTests
{
    [Theory]
    [InlineData("SUM(Sales[Amount]); DROP TABLE Sales")]
    [InlineData("EXEC xp_cmdshell 'dir'")]
    public void MeasureExpression_WithMaliciousPayload_IsRejected(string expression)
        => MeasureValidationRules.IsExpressionSafe(expression).Should().BeFalse();

    [Fact]
    public void MeasureExpression_WithSafeDax_IsAccepted()
        => MeasureValidationRules.IsExpressionSafe("SUM(Sales[Amount])").Should().BeTrue();
}
