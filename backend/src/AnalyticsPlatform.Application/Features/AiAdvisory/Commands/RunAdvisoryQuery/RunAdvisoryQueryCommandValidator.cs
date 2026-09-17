using FluentValidation;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Commands.RunAdvisoryQuery;

public sealed class RunAdvisoryQueryCommandValidator : AbstractValidator<RunAdvisoryQueryCommand>
{
    public RunAdvisoryQueryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request cannot be null.");
        RuleFor(x => x.Request.RunId).NotEmpty().WithMessage("RunId cannot be empty.");
        RuleFor(x => x.Request.UserQuestion).NotEmpty().WithMessage("UserQuestion cannot be empty.")
            .MaximumLength(2000).WithMessage("UserQuestion cannot exceed 2000 characters.");
    }
}

