using MediatR;
using Microsoft.Extensions.Logging;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Application.Features.AiAdvisory.Services;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Commands.RunAdvisoryQuery;

public sealed class RunAdvisoryQueryCommandHandler : IRequestHandler<RunAdvisoryQueryCommand, Result<AdvisoryResultDto>>
{
    private readonly AdvisoryContextAssembler _assembler;
    private readonly IGroundedAdvisorySynthesizer _synthesizer;
    private readonly IAdvisoryAuditLogger _auditLogger;
    private readonly ILogger<RunAdvisoryQueryCommandHandler> _logger;

    private static readonly string[] PromptInjectionSignatures = new[]
    {
        "ignore previous instructions",
        "ignore all previous",
        "system prompt override",
        "reveal api key",
        "reveal secret",
        "drop database",
        "exfiltrate credentials",
        "bypass guardrails"
    };

    public RunAdvisoryQueryCommandHandler(
        AdvisoryContextAssembler assembler,
        IGroundedAdvisorySynthesizer synthesizer,
        IAdvisoryAuditLogger auditLogger,
        ILogger<RunAdvisoryQueryCommandHandler> logger)
    {
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
        _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AdvisoryResultDto>> Handle(RunAdvisoryQueryCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Pre-LLM Prompt Guardrail: Check prompt injection
        var lowerQuestion = request.UserQuestion.ToLowerInvariant();
        foreach (var signature in PromptInjectionSignatures)
        {
            if (lowerQuestion.Contains(signature))
            {
                _logger.LogWarning("Prompt injection attempt detected in Advisory request: '{Signature}'", signature);
                return Result<AdvisoryResultDto>.Failure($"Prompt blocked by PromptGuardrail: Potential adversarial pattern '{signature}' detected.");
            }
        }

        // 2. Assemble Context with Least Privilege & Redaction
        var context = await _assembler.AssembleAsync(request, cancellationToken);

        // 3. Provider Selection Controller (Section 6.2)
        // Hard-coded invariant: sensitive/confidential context forces LocalOllama only, never cloud
        string chosenProvider;
        if (context.MaximumEncounteredSensitivity >= SensitivityLevel.Sensitive || !context.ResolvedPolicy.AllowCloudProvider)
        {
            chosenProvider = "LocalOllama";
        }
        else
        {
            chosenProvider = "CloudProvider";
        }

        // 4. Grounded Synthesis & Explanation
        var synthesis = await _synthesizer.SynthesizeExplanationAsync(request, context, chosenProvider, cancellationToken);

        // 5. Response Guardrail: Citation Validation (Section 6.3)
        // Citations MUST strictly match rule IDs and run IDs from the assembled context
        var validCitedRules = synthesis.CitedRuleIds
            .Where(id => context.AllowlistedRuleIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var validCitedRuns = synthesis.CitedRunIds
            .Where(id => context.AllowlistedRunIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 6. Create Domain AdvisoryResult
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();
        var advisoryResultCreation = AdvisoryResult.Create(
            request.RunId,
            request.QuestionType,
            request.UserQuestion,
            synthesis.Answer,
            chosenProvider,
            validCitedRules,
            validCitedRuns,
            context.RedactedFieldCount,
            request.UnlockConfidential,
            correlationId);

        if (!advisoryResultCreation.IsSuccess || advisoryResultCreation.Value == null)
        {
            return Result<AdvisoryResultDto>.Failure(advisoryResultCreation.Errors);
        }

        var result = advisoryResultCreation.Value;

        // 7. Telemetry & Audit Logging (Section 6.4)
        await _auditLogger.LogAdvisoryQueryAsync(
            result,
            context.ResolvedPolicy.PolicyName,
            chosenProvider,
            totalFieldsInspected: 10,
            redactedCount: context.RedactedFieldCount,
            cancellationToken);

        var dto = new AdvisoryResultDto(
            result.Id,
            result.RunId,
            result.QuestionType,
            result.UserQuestion,
            result.Answer,
            result.ProviderUsed,
            result.CitedRuleIds,
            result.CitedRunIds,
            result.RedactedFieldsCount,
            result.IsUnlockedConfidential,
            result.CorrelationId,
            result.TimestampUtc);

        return Result<AdvisoryResultDto>.Success(dto);
    }
}

