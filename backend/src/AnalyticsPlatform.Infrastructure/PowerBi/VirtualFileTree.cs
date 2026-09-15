using System.IO.Compression;
using System.Text;
using AnalyticsPlatform.Application.Common.Interfaces;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class VirtualFileTree : IVirtualFileTree
{
    private readonly Dictionary<string, VirtualFile> _files = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<VirtualFile> Files => _files.Values.ToList();

    public void AddTextFile(string relativePath, string content)
    {
        var normalizedPath = NormalizePath(relativePath);
        _files[normalizedPath] = new VirtualFile(normalizedPath, content);
    }

    public void AddBinaryFile(string relativePath, byte[] bytes)
    {
        var normalizedPath = NormalizePath(relativePath);
        _files[normalizedPath] = new VirtualFile(normalizedPath, string.Empty, bytes);
    }

    public string? GetContent(string relativePath)
    {
        var normalized = NormalizePath(relativePath);
        return _files.TryGetValue(normalized, out var file) ? file.Content : null;
    }

    public bool ContainsFile(string relativePath)
    {
        var normalized = NormalizePath(relativePath);
        return _files.ContainsKey(normalized);
    }

    public byte[] ToZipArchive()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in _files.Values)
            {
                var entry = archive.CreateEntry(file.RelativePath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                if (file.BinaryContent != null && file.BinaryContent.Length > 0)
                {
                    entryStream.Write(file.BinaryContent, 0, file.BinaryContent.Length);
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(file.Content);
                    entryStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        return memoryStream.ToArray();
    }

    public void WriteToDirectory(string targetRootDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetRootDirectory))
            throw new ArgumentException("Target root directory cannot be empty.", nameof(targetRootDirectory));

        var fullTargetRoot = Path.GetFullPath(targetRootDirectory);
        Directory.CreateDirectory(fullTargetRoot);

        foreach (var file in _files.Values)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(fullTargetRoot, file.RelativePath));

            // Guard against directory traversal attacks (Path Traversal Prevention)
            if (!destinationPath.StartsWith(fullTargetRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Potential directory traversal detected for file '{file.RelativePath}'.");
            }

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (file.BinaryContent != null && file.BinaryContent.Length > 0)
            {
                File.WriteAllBytes(destinationPath, file.BinaryContent);
            }
            else
            {
                File.WriteAllText(destinationPath, file.Content, Encoding.UTF8);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}

