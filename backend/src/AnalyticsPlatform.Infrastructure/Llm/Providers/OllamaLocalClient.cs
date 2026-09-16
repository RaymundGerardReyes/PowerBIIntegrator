using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;

namespace AnalyticsPlatform.Infrastructure.Llm.Providers;

public sealed class OllamaLocalClient : IOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLocalClient> _logger;

    public OllamaLocalClient(HttpClient httpClient, ILogger<OllamaLocalClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string model, string prompt, string? jsonFormat = null, CancellationToken ct = default)
    {
        var request = new OllamaChatRequest(
            Model: model,
            Messages: new[] { new OllamaChatMessage("user", prompt) },
            Stream: false,
            Format: jsonFormat
        );

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chat", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
            return result?.Message?.Content ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with local Ollama service at {BaseAddress}", _httpClient.BaseAddress);
            throw new InvalidOperationException($"Ollama local service communication error: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        string model,
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new OllamaChatRequest(
            Model: model,
            Messages: new[] { new OllamaChatMessage("user", prompt) },
            Stream: true
        );

        HttpResponseMessage response;
        try
        {
            var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request)
            };
            response = await _httpClient.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to initiate stream with local Ollama service at {BaseAddress}", _httpClient.BaseAddress);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            }
            catch (JsonException)
            {
                // Non-JSON line or partial line
            }

            if (chunk?.Message?.Content is { Length: > 0 } content)
            {
                yield return content;
            }

            if (chunk?.Done == true)
            {
                yield break;
            }
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/version", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

