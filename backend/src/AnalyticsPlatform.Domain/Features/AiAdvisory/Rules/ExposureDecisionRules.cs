using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;
using AnalyticsPlatform.Domain.Features.AiAdvisory.ValueObjects;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.AiAdvisory.Rules;

public static class ExposureDecisionRules
{
    public static ExposureDecision EvaluateExposure(
        SensitivityLevel fieldSensitivity,
        string fieldType,
        AdvisoryPolicy policy,
        bool hasExplicitHumanUnlock = false)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Rule 1: Restricted data has a hard-coded ceiling. NEVER eligible for any LLM under any condition.
        if (fieldSensitivity == SensitivityLevel.Restricted)
        {
            return ExposureDecision.BlockRestricted();
        }

        // Rule 2: If sensitive/confidential and requires human approval, check if explicitly unlocked
        if (fieldSensitivity == SensitivityLevel.Sensitive && policy.RequireHumanApprovalForConfidential && !hasExplicitHumanUnlock)
        {
            return ExposureDecision.HoldPendingApproval(fieldType);
        }

        // Rule 3: If sensitivity exceeds policy max exposure level, redact it
        if (fieldSensitivity > policy.MaxExposureLevel)
        {
            return ExposureDecision.Redact(fieldType);
        }

        // Rule 4: Otherwise, include as-is (subject to downstream PII guardrails)
        return ExposureDecision.Include();
    }
}
