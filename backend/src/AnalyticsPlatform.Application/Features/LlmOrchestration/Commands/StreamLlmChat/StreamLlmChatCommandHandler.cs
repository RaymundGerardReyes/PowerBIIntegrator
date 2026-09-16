using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.StreamLlmChat;

public sealed class StreamLlmChatCommandHandler : IRequestHandler<StreamLlmChatCommand, IAsyncEnumerable<string>>
{
    private readonly IOllamaClient _ollamaClient;
    private readonly ILogger<StreamLlmChatCommandHandler> _logger;

    public StreamLlmChatCommandHandler(IOllamaClient ollamaClient, ILogger<StreamLlmChatCommandHandler> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public Task<IAsyncEnumerable<string>> Handle(StreamLlmChatCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[StreamLlmChatCommandHandler] Streaming tokens for prompt with correlation ID {CorrelationId}",
            request.CorrelationId);

        var stream = _ollamaClient.StreamChatAsync("llama3.3:8b-instruct", request.SanitizedPrompt, cancellationToken);
        return Task.FromResult(stream);
    }
}
