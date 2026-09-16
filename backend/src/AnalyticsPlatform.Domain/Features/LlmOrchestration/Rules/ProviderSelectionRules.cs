using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;

public static class ProviderSelectionRules
{
    public static Result<LlmProviderType> ResolveProvider(
        LlmPolicy policy,
        SensitivityLevel taskSensitivity,
        LlmProviderType requestedPreference,
        bool userHasCloudPrivilege)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // Rule 1: Restricted or Sensitive data NEVER leaves the local perimeter
        if (taskSensitivity >= SensitivityLevel.Sensitive)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 2: If sensitivity exceeds policy maximum, force local
        if (taskSensitivity > policy.MaximumAllowedSensitivity)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 3: If policy explicitly forbids cloud, force LocalOllama regardless of request
        if (!policy.AllowCloudProvider)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 4: User role privilege check
        if (!userHasCloudPrivilege && requestedPreference != LlmProviderType.LocalOllama)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 5: If preference is valid under policy, honor it
        return Result<LlmProviderType>.Success(requestedPreference);
    }
}

