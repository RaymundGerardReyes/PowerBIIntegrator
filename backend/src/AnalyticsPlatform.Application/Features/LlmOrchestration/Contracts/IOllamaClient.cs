namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public interface IOllamaClient
{
    Task<string> ChatAsync(string model, string prompt, string? jsonFormat = null, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamChatAsync(string model, string prompt, CancellationToken ct = default);
    Task<bool> CheckHealthAsync(CancellationToken ct = default);
}

