using FluentValidation;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.StreamLlmChat;

public sealed class StreamLlmChatCommandValidator : AbstractValidator<StreamLlmChatCommand>
{
    public StreamLlmChatCommandValidator()
    {
        RuleFor(x => x.UserPrompt).NotEmpty().WithMessage("UserPrompt is required.");
    }
}

