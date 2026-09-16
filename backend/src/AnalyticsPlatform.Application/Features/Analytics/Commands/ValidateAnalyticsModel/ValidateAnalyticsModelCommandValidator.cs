using FluentValidation;

namespace AnalyticsPlatform.Application.Features.Analytics.Commands.ValidateAnalyticsModel;

public class ValidateAnalyticsModelCommandValidator : AbstractValidator<ValidateAnalyticsModelCommand>
{
    public ValidateAnalyticsModelCommandValidator()
    {
        RuleFor(x => x.AnalyticsModelId)
            .NotEmpty()
            .WithMessage("AnalyticsModelId must be a valid non-empty GUID.");
    }
}
