namespace AnalyticsPlatform.Application.Common.Interfaces;

public sealed record LocalPowerBiStatus(
    bool IsInstalled,
    string? DesktopExecutablePath,
    bool IsRunning,
    int? ProcessId,
    int? AnalysisServicesPort,
    string DefaultOutputDirectory,
    string InstallationType
);

public sealed record LaunchProjectResult(
    bool Success,
    string ProjectName,
    string PbipFilePath,
    string ProjectDirectory,
    bool LaunchedInDesktop,
    string? Message
);

public interface ILocalPowerBiDesktopService
{
    Task<LocalPowerBiStatus> GetStatusAsync(CancellationToken ct = default);
    Task<LaunchProjectResult> LaunchProjectAsync(
        string outputRoot,
        string projectName,
        IDictionary<string, string> reportFiles,
        IDictionary<string, string> semanticModelFiles,
        CancellationToken ct = default);
    bool OpenFolder(string folderPath);
}

