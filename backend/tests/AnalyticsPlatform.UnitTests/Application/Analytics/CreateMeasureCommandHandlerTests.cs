using FluentAssertions;
using AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.Analytics;

public class CreateMeasureCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsCreatedMeasureResponse()
    {
        var handler = new CreateMeasureCommandHandler();
        var command = new CreateMeasureCommand("TotalSales", "SUM(Sales[Revenue])", "Sales");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TotalSales");
    }
}
