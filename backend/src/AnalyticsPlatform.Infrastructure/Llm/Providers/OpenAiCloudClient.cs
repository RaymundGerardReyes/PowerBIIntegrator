using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using AnalyticsPlatform.Infrastructure.Llm.Resilience;
using Polly;

namespace AnalyticsPlatform.Infrastructure.Llm.Providers;

public sealed class OpenAiCloudClient : ICloudLlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiCloudClient> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public LlmProviderType ProviderType => LlmProviderType.CloudOpenAi;

    public OpenAiCloudClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiCloudClient> logger)
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
            throw new InvalidOperationException("OpenAI cloud API key is not configured.");
        }

        var requestPayload = new OpenAiChatCompletionRequest
        {
            Model = string.IsNullOrWhiteSpace(model) ? "gpt-4o" : model,
            Messages = new[]
            {
                new OpenAiChatMessage { Role = "user", Content = prompt }
            }
        };

        return await _resiliencePipeline.ExecuteAsync(async token =>
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = JsonContent.Create(requestPayload)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(httpRequest, token);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>(cancellationToken: token);
            return responseBody?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }, ct);
    }

    private string? GetApiKey()
    {
        return _configuration["Llm:OpenAiApiKey"] 
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private sealed class OpenAiChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4o";

        [JsonPropertyName("messages")]
        public OpenAiChatMessage[] Messages { get; set; } = Array.Empty<OpenAiChatMessage>();
    }

    private sealed class OpenAiChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OpenAiChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiChatMessage? Message { get; set; }
    }
}

