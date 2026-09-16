using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.ReportGeneration;

public class GenerateReportCommandValidatorsTests
{
    [Fact]
    public void GeneratePdfReportCommandValidator_WhenTitleEmpty_FailsValidation()
    {
        var validator = new GeneratePdfReportCommandValidator();
        var model = new ReportDocumentModel(string.Empty);
        var command = new GeneratePdfReportCommand(model);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Model.Title");
    }

    [Fact]
    public void GenerateExcelReportCommandValidator_WhenTitleEmpty_FailsValidation()
    {
        var validator = new GenerateExcelReportCommandValidator();
        var model = new ReportDocumentModel(string.Empty);
        var command = new GenerateExcelReportCommand(model);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Model.Title");
    }

    [Fact]
    public void GenerateWordReportCommandValidator_WhenTitleEmpty_FailsValidation()
    {
        var validator = new GenerateWordReportCommandValidator();
        var model = new ReportDocumentModel(string.Empty);
        var command = new GenerateWordReportCommand(model);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Model.Title");
    }

    [Fact]
    public void GeneratePdfReportCommandValidator_WhenValid_PassesValidation()
    {
        var validator = new GeneratePdfReportCommandValidator();
        var model = new ReportDocumentModel("Executive Report", "Subtitle", "Author", "Org");
        var command = new GeneratePdfReportCommand(model);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
