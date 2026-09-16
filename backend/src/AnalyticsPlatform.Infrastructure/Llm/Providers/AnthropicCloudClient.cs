using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Llm.Resilience;
using Polly;

namespace AnalyticsPlatform.Infrastructure.Llm.Providers;

public sealed class AnthropicCloudClient : ICloudLlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicCloudClient> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public LlmProviderType ProviderType => LlmProviderType.CloudAnthropic;

    public AnthropicCloudClient(HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicCloudClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _resiliencePipeline = PollyLlmResilience.CreateLlmPipeline();
    }

    public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
    {
        var apiKey = GetApiKey();
        return Task.FromResult(!string.IsNullOrWhiteSpace(apiKey));
    }

    public async Task<string> ChatAsync(string model, string prompt, CancellationToken ct = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Anthropic cloud API key is not configured.");
        }

        var requestPayload = new AnthropicMessagesRequest
        {
            Model = string.IsNullOrWhiteSpace(model) ? "claude-3-5-sonnet-20241022" : model,
            MaxTokens = 1024,
            Messages = new[]
            {
                new AnthropicMessage { Role = "user", Content = prompt }
            }
        };

        return await _resiliencePipeline.ExecuteAsync(async (state, token) =>
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = JsonContent.Create(state)
            };
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest, token);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadFromJsonAsync<AnthropicMessagesResponse>(cancellationToken: token);
            return responseBody?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;
        }, requestPayload, ct);
    }

    private string? GetApiKey()
    {
        return _configuration["Llm:AnthropicApiKey"] 
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    private sealed class AnthropicMessagesRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "claude-3-5-sonnet-20241022";

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("messages")]
        public AnthropicMessage[] Messages { get; set; } = Array.Empty<AnthropicMessage>();
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AnthropicMessagesResponse
    {
        [JsonPropertyName("content")]
        public AnthropicContentBlock[]? Content { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}

