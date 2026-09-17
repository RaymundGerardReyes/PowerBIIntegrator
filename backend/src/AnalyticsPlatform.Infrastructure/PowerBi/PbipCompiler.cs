using System.Text.Json;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class PbipCompiler : IPbipCompiler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IPbirGenerator _pbirGenerator;
    private readonly ITmdlGenerator _tmdlGenerator;

    public PbipCompiler(IPbirGenerator pbirGenerator, ITmdlGenerator tmdlGenerator)
    {
        _pbirGenerator = pbirGenerator;
        _tmdlGenerator = tmdlGenerator;
    }

    public IVirtualFileTree CompileProject(string projectName, DashboardDefinition dashboard, AnalyticsModel model)
    {
        var sanitizedProjectName = SanitizeName(projectName);
        var projectTree = new VirtualFileTree();

        // 1. Root <ProjectName>.pbip descriptor
        var pbipDescriptor = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/pbip/pbipProperties/1.0.0/schema.json",
            ["version"] = "1.0",
            ["artifacts"] = new object[]
            {
                new
                {
                    report = new
                    {
                        path = $"{sanitizedProjectName}.Report"
                    }
                }
            },
            ["settings"] = new { }
        };

        var pbipJson = JsonSerializer.Serialize(pbipDescriptor, JsonOptions);
        projectTree.AddTextFile($"{sanitizedProjectName}.pbip", pbipJson);

        // 2. Compile PBIR Report definition under <ProjectName>.Report/
        var semanticModelRelativePath = $"../{sanitizedProjectName}.SemanticModel";
        var pbirTree = _pbirGenerator.GenerateReportDefinition(dashboard, semanticModelRelativePath);

        foreach (var file in pbirTree.Files)
        {
            var projectRelativePath = $"{sanitizedProjectName}.Report/{file.RelativePath}";
            if (file.BinaryContent != null && file.BinaryContent.Length > 0)
            {
                projectTree.AddBinaryFile(projectRelativePath, file.BinaryContent);
            }
            else
            {
                projectTree.AddTextFile(projectRelativePath, file.Content);
            }
        }

        // 3. Compile TMDL Semantic Model definition under <ProjectName>.SemanticModel/
        var tmdlTree = _tmdlGenerator.GenerateSemanticModel(model);

        foreach (var file in tmdlTree.Files)
        {
            var projectRelativePath = $"{sanitizedProjectName}.SemanticModel/{file.RelativePath}";
            if (file.BinaryContent != null && file.BinaryContent.Length > 0)
            {
                projectTree.AddBinaryFile(projectRelativePath, file.BinaryContent);
            }
            else
            {
                projectTree.AddTextFile(projectRelativePath, file.Content);
            }
        }

        return projectTree;
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "AnalyticsProject";

        var withoutExt = name;
        foreach (var ext in new[] { ".xls", ".xlsx", ".csv", ".json", ".pbip", ".pbix" })
        {
            if (withoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                withoutExt = withoutExt[..^ext.Length];
                break;
            }
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(withoutExt.Where(c => !invalid.Contains(c) && c != '/' && c != '\\' && c != '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "AnalyticsProject" : cleaned;
    }
}
