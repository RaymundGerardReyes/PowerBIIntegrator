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

        var reportDir = Path.Combine(canonicalRoot, $"{sanitizedProjectName}.Report", "definition");
        var modelDir = Path.Combine(canonicalRoot, $"{sanitizedProjectName}.SemanticModel", "definition");

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

        var pbipDescriptor = new
        {
            version = "1.0",
            artifacts = new object[]
            {
                new { report = new { path = $"{sanitizedProjectName}.Report" } }
            },
            settings = new { }
        };

        var pbipJson = JsonSerializer.Serialize(pbipDescriptor, JsonOptions);
        File.WriteAllText(Path.Combine(canonicalRoot, $"{sanitizedProjectName}.pbip"), pbipJson);
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !invalid.Contains(c) && c != '/' && c != '\\' && c != '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "AnalyticsProject" : cleaned;
    }
}
