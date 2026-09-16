using System.Collections.Concurrent;
using AnalyticsPlatform.Application.Features.LlmOrchestration.Contracts;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Infrastructure.Llm.Policy;

public sealed class LlmPolicyRepository : ILlmPolicyRepository
{
    private readonly ConcurrentDictionary<string, LlmPolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

    public LlmPolicyRepository()
    {
        // Seed default policies
        var defaultPolicy = new LlmPolicy(
            PolicyId: "default",
            Name: "Default Local-First Policy",
            AllowCloudProvider: false,
            AllowSensitiveContext: false,
            AllowedTools: new[] { "get_analytics_model", "get_dashboard_definition" },
            MaxTokensPerRequest: 2048,
            MaxDailyTokenBudget: 20000,
            MaximumAllowedSensitivity: SensitivityLevel.Internal
        );

        var analyticsNonsensitive = new LlmPolicy(
            PolicyId: "analytics-nonsensitive",
            Name: "Analytics Non-Sensitive (Cloud Allowed)",
            AllowCloudProvider: true,
            AllowSensitiveContext: false,
            AllowedTools: new[] { "get_analytics_model", "compile_pbir_definition", "get_dashboard_definition" },
            MaxTokensPerRequest: 4096,
            MaxDailyTokenBudget: 50000,
            MaximumAllowedSensitivity: SensitivityLevel.Internal
        );

        var cameraEventsSensitive = new LlmPolicy(
            PolicyId: "camera-events-sensitive",
            Name: "Camera & Surveillance Events (Strict Local Only)",
            AllowCloudProvider: false,
            AllowSensitiveContext: true,
            AllowedTools: new[] { "query_event_summary" },
            MaxTokensPerRequest: 1024,
            MaxDailyTokenBudget: 10000,
            MaximumAllowedSensitivity: SensitivityLevel.Sensitive
        );

        _policies[defaultPolicy.PolicyId] = defaultPolicy;
        _policies[analyticsNonsensitive.PolicyId] = analyticsNonsensitive;
        _policies[cameraEventsSensitive.PolicyId] = cameraEventsSensitive;
    }

    public Task<LlmPolicy> GetPolicyByIdAsync(string policyId, CancellationToken ct)
    {
        if (_policies.TryGetValue(policyId, out var policy))
        {
            return Task.FromResult(policy);
        }

        // Fallback to default if not found
        return Task.FromResult(_policies["default"]);
    }

    public Task<IReadOnlyList<LlmPolicy>> GetAllPoliciesAsync(CancellationToken ct)
    {
        IReadOnlyList<LlmPolicy> list = _policies.Values.ToList();
        return Task.FromResult(list);
    }

    public Task RegisterPolicyAsync(LlmPolicy policy, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policies[policy.PolicyId] = policy;
        return Task.CompletedTask;
    }
}
