using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Commands.StreamLlmChat;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.LlmOrchestration;

public sealed class StreamLlmChatCommandHandlerTests
{
    [Fact]
    public async Task Handle_StreamsTokensFromOllamaClient()
    {
        var mockOllama = Substitute.For<IOllamaClient>();
        mockOllama.StreamChatAsync("llama3.3:8b-instruct", "Explain measures", Arg.Any<CancellationToken>())
            .Returns(GetSimulatedStream(new[] { "Measure ", "explains ", "revenue." }));

        var handler = new StreamLlmChatCommandHandler(mockOllama, NullLogger<StreamLlmChatCommandHandler>.Instance);
        var command = new StreamLlmChatCommand("Explain measures");

        var tokenStream = await handler.Handle(command, CancellationToken.None);

        var tokens = new List<string>();
        await foreach (var token in tokenStream)
        {
            tokens.Add(token);
        }

        tokens.Should().ContainInOrder("Measure ", "explains ", "revenue.");
    }

    private static async IAsyncEnumerable<string> GetSimulatedStream(IEnumerable<string> tokens)
    {
        foreach (var t in tokens)
        {
            yield return t;
            await Task.Yield();
        }
    }
}

