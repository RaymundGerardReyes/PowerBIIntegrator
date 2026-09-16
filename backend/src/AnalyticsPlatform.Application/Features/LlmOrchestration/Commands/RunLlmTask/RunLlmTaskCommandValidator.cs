using FluentValidation;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.RunLlmTask;

public sealed class RunLlmTaskCommandValidator : AbstractValidator<RunLlmTaskCommand>
{
    public RunLlmTaskCommandValidator()
    {
        RuleFor(x => x.TaskType).NotEmpty().WithMessage("TaskType cannot be empty.");
        RuleFor(x => x.UserPrompt).NotEmpty().WithMessage("UserPrompt cannot be empty.");
        RuleFor(x => x.PolicyId).NotEmpty().WithMessage("PolicyId cannot be empty.");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId cannot be empty.");
    }
}
