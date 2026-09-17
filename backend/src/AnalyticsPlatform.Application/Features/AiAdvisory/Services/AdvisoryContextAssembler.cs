using System.Text.Json;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;
using AnalyticsPlatform.Domain.Features.AiAdvisory.Rules;
using AnalyticsPlatform.Domain.Features.AiAdvisory.ValueObjects;
using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Application.Features.AiAdvisory.Services;

public sealed record AssembledAdvisoryContext(
    string FormattedContextJson,
    IReadOnlyList<string> AllowlistedRuleIds,
    IReadOnlyList<string> AllowlistedRunIds,
    int RedactedFieldCount,
    SensitivityLevel MaximumEncounteredSensitivity,
    AdvisoryPolicy ResolvedPolicy);

public class AdvisoryContextAssembler
{
    private readonly IAdvisoryToolRegistry _toolRegistry;

    public AdvisoryContextAssembler(IAdvisoryToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public virtual async Task<AssembledAdvisoryContext> AssembleAsync(
        AdvisoryQueryRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Resolve Policy based on user role and request flags
        var policy = ResolvePolicy(request.UserRole, request.UnlockConfidential);

        // 2. Resolve Allowed Read-Only Tools
        var tools = _toolRegistry.GetAllowedTools(policy.AllowedTools);

        var toolOutputs = new List<object>();
        var ruleIds = new HashSet<string>();
        var runIds = new HashSet<string> { request.RunId };
        int redactedCount = 0;
        var maxSensitivity = SensitivityLevel.Public;

        // 3. Execute tools and collect structured data
        foreach (var tool in tools)
        {
            var result = await tool.ExecuteAsync(request.RunId, ct);
            if (result.Success)
            {
                foreach (var rId in result.GroundedRuleIds) ruleIds.Add(rId);
                foreach (var runId in result.GroundedRunIds) runIds.Add(runId);

                // Check exposure decision for tool payload
                var toolSensitivity = InferToolSensitivity(tool.Name);
                if (toolSensitivity > maxSensitivity)
                {
                    maxSensitivity = toolSensitivity;
                }

                var decision = ExposureDecisionRules.EvaluateExposure(
                    toolSensitivity,
                    tool.Name,
                    policy,
                    request.UnlockConfidential);

                if (decision.Decision == ExposureDecisionType.BlockedRestricted)
                {
                    redactedCount += result.TotalFieldsInspected;
                    continue; // Skip entirely
                }

                if (decision.Decision == ExposureDecisionType.Redacted || decision.Decision == ExposureDecisionType.HeldPendingApproval)
                {
                    redactedCount += result.TotalFieldsInspected;
                    toolOutputs.Add(new
                    {
                        tool = tool.Name,
                        description = tool.Description,
                        data = decision.MaskedPlaceholder,
                        reason = decision.Reason
                    });
                }
                else
                {
                    toolOutputs.Add(new
                    {
                        tool = tool.Name,
                        description = tool.Description,
                        data = result.Data
                    });
                }
            }
        }

        // 4. Assemble structured context JSON
        var structuredContext = new
        {
            queryContext = new
            {
                runId = request.RunId,
                questionType = request.QuestionType,
                userQuestion = request.UserQuestion,
                policyApplied = policy.PolicyName,
                isUnlockedConfidential = request.UnlockConfidential
            },
            verifiedGroundedRules = ruleIds.ToList(),
            toolExecutionResults = toolOutputs
        };

        var json = JsonSerializer.Serialize(structuredContext, s_jsonOptions);

        return new AssembledAdvisoryContext(
            json,
            ruleIds.ToList(),
            runIds.ToList(),
            redactedCount,
            maxSensitivity,
            policy);
    }

    private static AdvisoryPolicy ResolvePolicy(string? userRole, bool isUnlocked)
    {
        if (isUnlocked && string.Equals(userRole, "DataSteward", StringComparison.OrdinalIgnoreCase))
        {
            return AdvisoryPolicy.DataStewardElevated();
        }

        return AdvisoryPolicy.Default();
    }

    private static SensitivityLevel InferToolSensitivity(string toolName) => toolName switch
    {
        "get_duplicate_clusters" => SensitivityLevel.Sensitive, // Has row-level identifiers
        "get_dataset_profile" => SensitivityLevel.Internal,
        "get_schema_violations" => SensitivityLevel.Internal,
        "get_pipeline_run_result" => SensitivityLevel.Public,
        "get_transformation_plan" => SensitivityLevel.Internal,
        "get_chart_suggestions" => SensitivityLevel.Public,
        _ => SensitivityLevel.Internal
    };
}

