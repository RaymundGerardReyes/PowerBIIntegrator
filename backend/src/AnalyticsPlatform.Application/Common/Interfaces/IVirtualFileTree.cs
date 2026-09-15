namespace AnalyticsPlatform.Application.Common.Interfaces;

public sealed record VirtualFile(string RelativePath, string Content, byte[]? BinaryContent = null);

public interface IVirtualFileTree
{
    IReadOnlyList<VirtualFile> Files { get; }
    void AddTextFile(string relativePath, string content);
    void AddBinaryFile(string relativePath, byte[] bytes);
    byte[] ToZipArchive();
    void WriteToDirectory(string targetRootDirectory);
    string? GetContent(string relativePath);
    bool ContainsFile(string relativePath);
}

