using FluentAssertions;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Domain.LlmOrchestration;

public class ProviderSelectionRulesTests
{
    private readonly LlmPolicy _permissivePolicy = new(
        PolicyId: "permissive-policy",
        Name: "Permissive Analytics Policy",
        AllowCloudProvider: true,
        AllowSensitiveContext: true,
        AllowedTools: new[] { "compile_pbir_definition", "get_analytics_model" },
        MaxTokensPerRequest: 4096,
        MaxDailyTokenBudget: 50000,
        MaximumAllowedSensitivity: SensitivityLevel.Internal
    );

    private readonly LlmPolicy _localOnlyPolicy = new(
        PolicyId: "local-only-policy",
        Name: "Strict Local-First Policy",
        AllowCloudProvider: false,
        AllowSensitiveContext: false,
        AllowedTools: new[] { "get_analytics_model" },
        MaxTokensPerRequest: 2048,
        MaxDailyTokenBudget: 10000,
        MaximumAllowedSensitivity: SensitivityLevel.Public
    );

    [Fact]
    public void ResolveProvider_WhenSensitivityIsSensitive_ForcesLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _permissivePolicy,
            SensitivityLevel.Sensitive,
            requestedPreference: LlmProviderType.CloudOpenAi,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }

    [Fact]
    public void ResolveProvider_WhenSensitivityIsRestricted_ForcesLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _permissivePolicy,
            SensitivityLevel.Restricted,
            requestedPreference: LlmProviderType.CloudAnthropic,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }

    [Fact]
    public void ResolveProvider_WhenPolicyForbidsCloud_ForcesLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _localOnlyPolicy,
            SensitivityLevel.Public,
            requestedPreference: LlmProviderType.CloudOpenAi,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }

    [Fact]
    public void ResolveProvider_WhenUserLacksCloudPrivilege_ForcesLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _permissivePolicy,
            SensitivityLevel.Public,
            requestedPreference: LlmProviderType.CloudOpenAi,
            userHasCloudPrivilege: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }

    [Fact]
    public void ResolveProvider_WhenTaskSensitivityExceedsPolicyMaximum_ForcesLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _localOnlyPolicy, // max allowed is Public
            SensitivityLevel.Internal,
            requestedPreference: LlmProviderType.CloudOpenAi,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }

    [Fact]
    public void ResolveProvider_WhenAllConditionsSatisfied_HonorsCloudPreference()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _permissivePolicy,
            SensitivityLevel.Internal,
            requestedPreference: LlmProviderType.CloudOpenAi,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.CloudOpenAi);
    }

    [Fact]
    public void ResolveProvider_WhenPreferenceIsLocalOllama_ReturnsLocalOllama()
    {
        var result = ProviderSelectionRules.ResolveProvider(
            _permissivePolicy,
            SensitivityLevel.Public,
            requestedPreference: LlmProviderType.LocalOllama,
            userHasCloudPrivilege: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LlmProviderType.LocalOllama);
    }
}
