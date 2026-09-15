using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.DataSources;

public class RegisterDataSourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ExtractsSchemaAndPersistsEntity()
    {
        var repository = Substitute.For<IDataSourceRepository>();
        var extractorFactory = Substitute.For<IDataSourceSchemaExtractorFactory>();
        var extractor = Substitute.For<IDataSourceSchemaExtractor>();

        var expectedSchema = new List<ColumnSchema>
        {
            new(0, "Id", ColumnDataType.Integer, false, new[] { "1", "2" }),
            new(1, "CustomerName", ColumnDataType.String, true, new[] { "Acme Corp" }),
            new(2, "Amount", ColumnDataType.Decimal, false, new[] { "100.50" }),
            new(3, "CreatedAt", ColumnDataType.DateTime, false, new[] { "2026-01-01T00:00:00Z" })
        };

        extractorFactory.GetExtractor(DataSourceType.Excel).Returns(extractor);
        extractor.ExtractSchemaAsync("C:\\data\\sales.xlsx", Arg.Any<CancellationToken>())
            .Returns(expectedSchema);

        var handler = new RegisterDataSourceCommandHandler(repository, extractorFactory);
        var command = new RegisterDataSourceCommand("SalesExcel", DataSourceType.Excel, "C:\\data\\sales.xlsx");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("SalesExcel");
        result.Value.Type.Should().Be("excel");
        result.Value.ConnectionOrPath.Should().Be("C:\\data\\sales.xlsx");
        result.Value.Schema.Should().HaveCount(4);
        result.Value.Schema[0].Name.Should().Be("Id");
        result.Value.Schema[0].InferredType.Should().Be(ColumnDataType.Integer);

        await repository.Received(1).AddAsync(Arg.Is<DataSourceDefinition>(d =>
            d.Name == "SalesExcel" &&
            d.Type == DataSourceType.Excel &&
            d.Schema.Count == 4), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidName_ReturnsFailureWithoutCallingExtractor(string? invalidName)
    {
        var repository = Substitute.For<IDataSourceRepository>();
        var extractorFactory = Substitute.For<IDataSourceSchemaExtractorFactory>();

        var handler = new RegisterDataSourceCommandHandler(repository, extractorFactory);
        var command = new RegisterDataSourceCommand(invalidName!, DataSourceType.Csv, "C:\\data.csv");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name"));
        extractorFactory.DidNotReceiveWithAnyArgs().GetExtractor(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidPath_ReturnsFailureWithoutCallingExtractor(string? invalidPath)
    {
        var repository = Substitute.For<IDataSourceRepository>();
        var extractorFactory = Substitute.For<IDataSourceSchemaExtractorFactory>();

        var handler = new RegisterDataSourceCommandHandler(repository, extractorFactory);
        var command = new RegisterDataSourceCommand("ValidName", DataSourceType.Csv, invalidPath!);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ConnectionOrPath"));
        extractorFactory.DidNotReceiveWithAnyArgs().GetExtractor(default);
    }

    [Fact]
    public async Task Handle_WhenExtractorThrowsException_ReturnsFailureGracefully()
    {
        var repository = Substitute.For<IDataSourceRepository>();
        var extractorFactory = Substitute.For<IDataSourceSchemaExtractorFactory>();
        var extractor = Substitute.For<IDataSourceSchemaExtractor>();

        extractorFactory.GetExtractor(DataSourceType.SqlServer).Returns(extractor);
        extractor.ExtractSchemaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection timeout"));

        var handler = new RegisterDataSourceCommandHandler(repository, extractorFactory);
        var command = new RegisterDataSourceCommand("SqlServerDb", DataSourceType.SqlServer, "Server=db;Database=test;");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Connection timeout"));
        await repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public void Validator_WithValidCommand_PassesValidation()
    {
        var validator = new RegisterDataSourceCommandValidator();
        var command = new RegisterDataSourceCommand("OrdersData", DataSourceType.Csv, "/data/orders.csv");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithEmptyFields_FailsValidation()
    {
        var validator = new RegisterDataSourceCommandValidator();
        var command = new RegisterDataSourceCommand(string.Empty, (DataSourceType)999, string.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "ConnectionOrPath");
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }
}

