using FluentAssertions;
using NSubstitute;
using Xunit;
using AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Repositories;

namespace AnalyticsPlatform.UnitTests.Application.Analytics;

public class ValidateAnalyticsModelCommandHandlerTests
{
    private readonly IAnalyticsModelRepository _repository = Substitute.For<IAnalyticsModelRepository>();

    [Fact]
    public async Task Handle_WhenModelExists_ReturnsValidationResponse()
    {
        var model = new AnalyticsModel("TestModel");
        model.GetOrAddTable("Table1");
        _repository.GetByIdAsync(model.Id, Arg.Any<CancellationToken>())
            .Returns(model);

        var handler = new ValidateAnalyticsModelCommandHandler(_repository);
        var command = new ValidateAnalyticsModelCommand(model.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ModelName.Should().Be("TestModel");
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenModelDoesNotExist_ReturnsFailure()
    {
        var nonExistentId = Guid.NewGuid();
        _repository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((AnalyticsModel?)null);

        var handler = new ValidateAnalyticsModelCommandHandler(_repository);
        var command = new ValidateAnalyticsModelCommand(nonExistentId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("was not found"));
    }

    [Fact]
    public void Validator_WithEmptyGuid_FailsValidation()
    {
        var validator = new ValidateAnalyticsModelCommandValidator();
        var command = new ValidateAnalyticsModelCommand(Guid.Empty);

        var validationResult = validator.Validate(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "AnalyticsModelId");
    }
}
