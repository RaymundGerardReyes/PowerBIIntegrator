using FluentValidation;

namespace AnalyticsPlatform.Application.Features.DataSources.Commands.RegisterDataSource;

public class RegisterDataSourceCommandValidator : AbstractValidator<RegisterDataSourceCommand>
{
    public RegisterDataSourceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Data source name is required.")
            .MaximumLength(200).WithMessage("Data source name must not exceed 200 characters.");

        RuleFor(x => x.ConnectionOrPath)
            .NotEmpty().WithMessage("Connection or path is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid data source type specified.");
    }
}

