using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Infrastructure.Llm.Guardrails;

public sealed partial class PiiRedactionService : IPromptGuardrailService
{
    private readonly ILogger<PiiRedactionService> _logger;

    private static readonly string[] JailbreakPatterns = new[]
    {
        "ignore all previous instructions",
        "ignore previous instructions",
        "system prompt:",
        "you are now in developer mode",
        "dan mode",
        "jailbreak",
        "reveal your secret key"
    };

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ipv4Regex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SsnRegex();

    public PiiRedactionService(ILogger<PiiRedactionService> logger)
    {
        _logger = logger;
    }

    public Task<GuardrailScanResult> ValidateAndSanitizeAsync(
        string prompt,
        IReadOnlyList<string> contextIds,
        LlmPolicy policy,
        string correlationId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Task.FromResult(new GuardrailScanResult(
                IsBlocked: false,
                BlockReason: null,
                SanitizedPrompt: string.Empty,
                Violations: Array.Empty<GuardrailViolation>()
            ));
        }

        var lower = prompt.ToLowerInvariant();
        foreach (var pattern in JailbreakPatterns)
        {
            if (lower.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[Prompt Guardrail] Jailbreak attempt detected: {Pattern} (CorrelationId: {CorrelationId})",
                    pattern, correlationId);

                return Task.FromResult(new GuardrailScanResult(
                    IsBlocked: true,
                    BlockReason: $"Adversarial prompt injection pattern detected: '{pattern}'",
                    SanitizedPrompt: string.Empty,
                    Violations: new[]
                    {
                        new GuardrailViolation("PROMPT_INJECTION", $"Pattern '{pattern}' matched jailbreak signature", SensitivityLevel.Restricted)
                    }
                ));
            }
        }

        var violations = new List<GuardrailViolation>();
        var sanitized = prompt;

        // Redact and pseudonymize emails
        int emailIdx = 1;
        sanitized = EmailRegex().Replace(sanitized, m =>
        {
            violations.Add(new GuardrailViolation("PII_EMAIL", "Email address redacted", SensitivityLevel.Sensitive));
            return $"{{{{EMAIL_{emailIdx++}}}}}";
        });

        // Redact and pseudonymize IPv4 addresses
        int ipIdx = 1;
        sanitized = Ipv4Regex().Replace(sanitized, m =>
        {
            violations.Add(new GuardrailViolation("PII_IPV4", "IP address redacted", SensitivityLevel.Sensitive));
            return $"{{{{IP_{ipIdx++}}}}}";
        });

        // Redact SSN
        int ssnIdx = 1;
        sanitized = SsnRegex().Replace(sanitized, m =>
        {
            violations.Add(new GuardrailViolation("PII_SSN", "SSN redacted", SensitivityLevel.Restricted));
            return $"{{{{SSN_{ssnIdx++}}}}}";
        });

        if (violations.Count > 0)
        {
            _logger.LogInformation("[Prompt Guardrail] Sanitized {Count} PII entities (CorrelationId: {CorrelationId})",
                violations.Count, correlationId);
        }

        return Task.FromResult(new GuardrailScanResult(
            IsBlocked: false,
            BlockReason: null,
            SanitizedPrompt: sanitized,
            Violations: violations
        ));
    }
}
