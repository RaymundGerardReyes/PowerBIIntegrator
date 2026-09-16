using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

public interface ICloudLlmClient
{
    LlmProviderType ProviderType { get; }
    Task<string> ChatAsync(string model, string prompt, CancellationToken ct = default);
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);
}

