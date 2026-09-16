using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace AnalyticsPlatform.SecurityTests.Architecture;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(AnalyticsPlatform.Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "AnalyticsPlatform.Application",
                "AnalyticsPlatform.Infrastructure",
                "AnalyticsPlatform.Api")
                "AnalyticsPlatform.Api",
                "AnalyticsPlatform.McpServer")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
