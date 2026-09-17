using AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

namespace AnalyticsPlatform.Domain.Features.AiAdvisory.Entities;

public sealed class AdvisoryPolicy
{
    public string PolicyName { get; init; } = "Default";
    public bool AllowCloudProvider { get; init; }
    public SensitivityLevel MaxExposureLevel { get; init; } = SensitivityLevel.Internal;
    public IReadOnlySet<string> AllowedTools { get; init; } = new HashSet<string>
    {
        "get_pipeline_run_result",
        "get_dataset_profile",
        "get_duplicate_clusters",
        "get_schema_violations",
        "get_transformation_plan",
        "get_chart_suggestions"
    };
    public bool RequireHumanApprovalForConfidential { get; init; } = true;

    public static AdvisoryPolicy Default() => new()
    {
        PolicyName = "Default",
        AllowCloudProvider = false,
        MaxExposureLevel = SensitivityLevel.Internal,
        RequireHumanApprovalForConfidential = true
    };

    public static AdvisoryPolicy StrictLocal() => new()
    {
        PolicyName = "StrictLocal",
        AllowCloudProvider = false,
        MaxExposureLevel = SensitivityLevel.Public,
        RequireHumanApprovalForConfidential = true
    };

    public static AdvisoryPolicy DataStewardElevated() => new()
    {
        PolicyName = "DataStewardElevated",
        AllowCloudProvider = false, // Even elevated steward role forces LocalOllama when viewing sensitive rows
        MaxExposureLevel = SensitivityLevel.Sensitive,
        RequireHumanApprovalForConfidential = false
    };
}

