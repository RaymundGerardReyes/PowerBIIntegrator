using NSubstitute;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.ReportGeneration;

public class GenerateReportCommandHandlerTests
{
    private readonly IPdfReportGenerator _pdfGenerator = Substitute.For<IPdfReportGenerator>();
    private readonly IExcelReportGenerator _excelGenerator = Substitute.For<IExcelReportGenerator>();
    private readonly IWordReportGenerator _wordGenerator = Substitute.For<IWordReportGenerator>();

    [Fact]
    public async Task Handle_PdfCommand_WhenGeneratorSucceeds_ReturnsSuccessResult()
    {
        var model = new ReportDocumentModel("Q3 Summary");
        var fakeBytes = new byte[] { 1, 2, 3, 4 };
        _pdfGenerator.GenerateAsync(model, Arg.Any<CancellationToken>()).Returns(Task.FromResult(fakeBytes));

        var handler = new GeneratePdfReportCommandHandler(_pdfGenerator);
        var result = await handler.Handle(new GeneratePdfReportCommand(model), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContentType.Should().Be("application/pdf");
        result.Value.Content.Should().BeEquivalentTo(fakeBytes);
        result.Value.FileName.Should().Contain("Q3_Summary");
    }

    [Fact]
    public async Task Handle_ExcelCommand_WhenGeneratorSucceeds_ReturnsSuccessResult()
    {
        var model = new ReportDocumentModel("Financial Overview");
        var fakeBytes = new byte[] { 5, 6, 7, 8 };
        _excelGenerator.GenerateAsync(model, Arg.Any<CancellationToken>()).Returns(Task.FromResult(fakeBytes));

        var handler = new GenerateExcelReportCommandHandler(_excelGenerator);
        var result = await handler.Handle(new GenerateExcelReportCommand(model), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.Content.Should().BeEquivalentTo(fakeBytes);
        result.Value.FileName.Should().Contain("Financial_Overview");
    }

    [Fact]
    public async Task Handle_WordCommand_WhenGeneratorSucceeds_ReturnsSuccessResult()
    {
        var model = new ReportDocumentModel("Board Memo");
        var fakeBytes = new byte[] { 9, 10, 11, 12 };
        _wordGenerator.GenerateAsync(model, Arg.Any<CancellationToken>()).Returns(Task.FromResult(fakeBytes));

        var handler = new GenerateWordReportCommandHandler(_wordGenerator);
        var result = await handler.Handle(new GenerateWordReportCommand(model), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        result.Value.Content.Should().BeEquivalentTo(fakeBytes);
        result.Value.FileName.Should().Contain("Board_Memo");
    }

    [Fact]
    public async Task Handle_PdfCommand_WhenGeneratorThrows_ReturnsFailureResult()
    {
        var model = new ReportDocumentModel("Error Test");
        _pdfGenerator.GenerateAsync(model, Arg.Any<CancellationToken>()).Returns<byte[]>(_ => throw new InvalidOperationException("QuestPDF rendering error"));

        var handler = new GeneratePdfReportCommandHandler(_pdfGenerator);
        var result = await handler.Handle(new GeneratePdfReportCommand(model), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("QuestPDF rendering error"));
    }
}
