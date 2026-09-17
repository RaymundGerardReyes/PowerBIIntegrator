using System.Text.Json;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class PbipPackager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void WriteProject(string outputRoot, string projectName, IDictionary<string, string> reportFiles, IDictionary<string, string> semanticModelFiles)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new ArgumentException("Output root cannot be empty.", nameof(outputRoot));

        var sanitizedProjectName = SanitizeName(projectName);
        var canonicalRoot = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(canonicalRoot);

        var reportDir = Path.Combine(canonicalRoot, $"{sanitizedProjectName}.Report");
        var modelDir = Path.Combine(canonicalRoot, $"{sanitizedProjectName}.SemanticModel");

        Directory.CreateDirectory(reportDir);
        Directory.CreateDirectory(modelDir);

        foreach (var (relativePath, content) in reportFiles)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(reportDir, relativePath));
            if (!destinationPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Directory traversal attempt detected in path '{relativePath}'.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllText(destinationPath, content);
        }

        foreach (var (relativePath, content) in semanticModelFiles)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(modelDir, relativePath));
            if (!destinationPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Directory traversal attempt detected in path '{relativePath}'.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.WriteAllText(destinationPath, content);
        }

        var pbipDescriptor = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/pbip/pbipProperties/1.0.0/schema.json",
            ["version"] = "1.0",
            ["artifacts"] = new object[]
            {
                new { report = new { path = $"{sanitizedProjectName}.Report" } }
            },
            ["settings"] = new { }
        };

        var pbipJson = JsonSerializer.Serialize(pbipDescriptor, JsonOptions);
        File.WriteAllText(Path.Combine(canonicalRoot, $"{sanitizedProjectName}.pbip"), pbipJson);
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
