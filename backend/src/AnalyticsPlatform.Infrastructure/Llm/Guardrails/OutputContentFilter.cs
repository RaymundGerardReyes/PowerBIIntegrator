using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Infrastructure.Llm.Guardrails;

public sealed partial class OutputContentFilter : IResponseGuardrailService
{
    private readonly ILogger<OutputContentFilter> _logger;

    [GeneratedRegex(@"AKIA[0-9A-Z]{16}", RegexOptions.CultureInvariant)]
    private static partial Regex AwsKeyRegex();

    [GeneratedRegex(@"sk-[a-zA-Z0-9]{32,}", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyRegex();

    public OutputContentFilter(ILogger<OutputContentFilter> logger)
    {
        _logger = logger;
    }

    public Task<ResponseFilterResult> FilterAsync(
        string rawText,
        LlmPolicy policy,
        string correlationId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Task.FromResult(new ResponseFilterResult(
                IsBlocked: false,
                BlockReason: null,
                FilteredText: string.Empty,
                GuardrailNotice: null
            ));
        }

        // Check for leaked API secrets in response
        if (AwsKeyRegex().IsMatch(rawText) || ApiKeyRegex().IsMatch(rawText))
        {
            _logger.LogCritical("[Response Filter] Blocked response containing leaked API secret or key! (CorrelationId: {CorrelationId})",
                correlationId);

            return Task.FromResult(new ResponseFilterResult(
                IsBlocked: true,
                BlockReason: "Response blocked due to detected credential or API key leakage.",
                FilteredText: string.Empty,
                GuardrailNotice: "Content blocked by security filter: sensitive credentials detected."
            ));
        }

        return Task.FromResult(new ResponseFilterResult(
            IsBlocked: false,
            BlockReason: null,
            FilteredText: rawText,
            GuardrailNotice: null
        ));
    }
}
