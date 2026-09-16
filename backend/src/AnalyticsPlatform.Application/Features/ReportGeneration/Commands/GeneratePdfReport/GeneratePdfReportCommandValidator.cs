using FluentValidation;

namespace AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;

public sealed class GeneratePdfReportCommandValidator : AbstractValidator<GeneratePdfReportCommand>
{
    public GeneratePdfReportCommandValidator()
    {
        RuleFor(x => x.Model).NotNull().WithMessage("Report model cannot be null.");
        RuleFor(x => x.Model.Title)
            .NotEmpty().WithMessage("Report title is required.")
            .MaximumLength(200).WithMessage("Report title cannot exceed 200 characters.");

        RuleFor(x => x.Model.Author)
            .MaximumLength(100).WithMessage("Author cannot exceed 100 characters.");

        RuleFor(x => x.Model.Organization)
            .MaximumLength(150).WithMessage("Organization cannot exceed 150 characters.");

        RuleFor(x => x.Model.Sections)
            .Must(sections => sections == null || sections.Count <= 100)
            .WithMessage("Report cannot contain more than 100 sections.");
    }
}
