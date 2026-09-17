using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;

namespace AnalyticsPlatform.Infrastructure.Features.AiAdvisory.ToolRegistry;

public sealed class AdvisoryToolRegistry : IAdvisoryToolRegistry
{
    private readonly Dictionary<string, IAdvisoryTool> _tools;

    public AdvisoryToolRegistry(IEnumerable<IAdvisoryTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IAdvisoryTool> GetAllowedTools(IReadOnlySet<string> allowedToolNames)
    {
        return _tools.Values
            .Where(t => allowedToolNames.Contains(t.Name))
            .ToList();
    }

    public IAdvisoryTool? GetTool(string toolName)
    {
        _tools.TryGetValue(toolName, out var tool);
        return tool;
    }
}

