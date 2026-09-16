using FluentValidation;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RegisterLlmPolicy;

public sealed class RegisterLlmPolicyCommandValidator : AbstractValidator<RegisterLlmPolicyCommand>
{
    public RegisterLlmPolicyCommandValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty().WithMessage("PolicyId cannot be empty.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Policy name cannot be empty.");
        RuleFor(x => x.MaxTokensPerRequest).GreaterThan(0).WithMessage("MaxTokensPerRequest must be greater than zero.");
        RuleFor(x => x.MaxDailyTokenBudget).GreaterThan(0).WithMessage("MaxDailyTokenBudget must be greater than zero.");
    }
}

