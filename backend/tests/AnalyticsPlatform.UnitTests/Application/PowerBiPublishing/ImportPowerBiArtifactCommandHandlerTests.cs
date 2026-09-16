using FluentAssertions;
using NSubstitute;
using Xunit;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.ImportPowerBiArtifact;

namespace AnalyticsPlatform.UnitTests.Application.PowerBiPublishing;

public class ImportPowerBiArtifactCommandHandlerTests
{
    private readonly IPowerBiPublisher _publisher = Substitute.For<IPowerBiPublisher>();

    [Fact]
    public async Task Handle_WithValidArtifact_ReturnsImportSuccess()
    {
        _publisher.ImportArtifactAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns("import-xyz-123");

        var handler = new ImportPowerBiArtifactCommandHandler(_publisher);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var command = new ImportPowerBiArtifactCommand("ws-100", "SalesDataset", stream, "Sales.xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ImportId.Should().Be("import-xyz-123");
        result.Value.WorkspaceId.Should().Be("ws-100");
    }

    [Theory]
    [InlineData("", "SalesDataset", "Sales.xlsx")]
    [InlineData("ws-100", "", "Sales.xlsx")]
    public async Task Handle_WithEmptyRequiredFields_ReturnsFailure(string workspaceId, string datasetDisplayName, string fileName)
    {
        var handler = new ImportPowerBiArtifactCommandHandler(_publisher);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new ImportPowerBiArtifactCommand(workspaceId, datasetDisplayName, stream, fileName);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("file.exe")]
    [InlineData("script.sh")]
    [InlineData("document.pdf")]
    public void Validator_WithUnsupportedExtension_FailsValidation(string invalidFileName)
    {
        var validator = new ImportPowerBiArtifactCommandValidator();
        using var stream = new MemoryStream(new byte[] { 1 });
        var command = new ImportPowerBiArtifactCommand("ws-1", "Dataset", stream, invalidFileName);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Theory]
    [InlineData("report.pbix")]
    [InlineData("data.xlsx")]
    [InlineData("paginated.rdl")]
    [InlineData("definition.json")]
    public void Validator_WithSupportedExtensions_PassesValidation(string validFileName)
    {
        var validator = new ImportPowerBiArtifactCommandValidator();
        using var stream = new MemoryStream(new byte[] { 1 });
        var command = new ImportPowerBiArtifactCommand("ws-1", "Dataset", stream, validFileName);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
