using FluentValidation;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.CreateMeasure;

public class CreateMeasureCommandValidator : AbstractValidator<CreateMeasureCommand>
{
    public CreateMeasureCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Expression).NotEmpty();
        RuleFor(x => x.TableName).NotEmpty();
    }
}
