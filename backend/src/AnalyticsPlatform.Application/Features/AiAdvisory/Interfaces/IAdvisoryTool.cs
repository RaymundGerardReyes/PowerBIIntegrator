namespace AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

public sealed record AdvisoryToolResult(
    bool Success,
    string ToolName,
    object Data,
    IReadOnlyList<string> GroundedRuleIds,
    IReadOnlyList<string> GroundedRunIds,
    int TotalFieldsInspected);

public interface IAdvisoryTool
{
    string Name { get; }
    string Description { get; }
    Task<AdvisoryToolResult> ExecuteAsync(string runOrDatasetId, CancellationToken ct);
}

public interface IAdvisoryToolRegistry
{
    IReadOnlyList<IAdvisoryTool> GetAllowedTools(IReadOnlySet<string> allowedToolNames);
    IAdvisoryTool? GetTool(string toolName);
}

