using MediatR;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Common;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.StreamLlmChat;

public sealed record StreamLlmChatCommand(
    string UserPrompt,
    IReadOnlyList<string> ContextIds,
    string? ProviderPreference = null,
    string PolicyId = "default",
    string CorrelationId = ""
) : IRequest<IAsyncEnumerable<string>>, ILlmGuardedRequest
{
    public StreamLlmChatCommand() : this(string.Empty, Array.Empty<string>())
    {
    }

    public StreamLlmChatCommand(string userPrompt) : this(userPrompt, Array.Empty<string>())
    {
    }

    public string SanitizedPrompt { get; private set; } = UserPrompt;

    public void UpdateSanitizedPrompt(string sanitizedPrompt)
    {
        SanitizedPrompt = sanitizedPrompt;
    }
}

