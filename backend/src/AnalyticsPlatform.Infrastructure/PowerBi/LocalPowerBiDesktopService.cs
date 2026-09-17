using System.Diagnostics;
using System.Runtime.InteropServices;
using AnalyticsPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class LocalPowerBiDesktopService : ILocalPowerBiDesktopService
{
    private readonly PbipPackager _packager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalPowerBiDesktopService> _logger;

    private static readonly string[] PossibleStandalonePaths =
    [
        @"C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe",
        @"C:\Program Files (x86)\Microsoft Power BI Desktop\bin\PBIDesktop.exe"
    ];

    public LocalPowerBiDesktopService(
        PbipPackager packager,
        IConfiguration configuration,
        ILogger<LocalPowerBiDesktopService> logger)
    {
        _packager = packager;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<LocalPowerBiStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var (isInstalled, executablePath, installType) = DetectDesktopInstallation();

        var pbiProcesses = Process.GetProcessesByName("PBIDesktop");
        var isRunning = pbiProcesses.Length > 0;
        int? processId = isRunning ? pbiProcesses[0].Id : null;

        var msmdsrvProcesses = Process.GetProcessesByName("msmdsrv");
        int? analysisServicesPort = null;
        if (msmdsrvProcesses.Length > 0)
        {
            analysisServicesPort = TryDetectAnalysisServicesPort(msmdsrvProcesses[0].Id);
        }

        var defaultOutputDirectory = GetDefaultOutputDirectory();

        return Task.FromResult(new LocalPowerBiStatus(
            IsInstalled: isInstalled,
            DesktopExecutablePath: executablePath,
            IsRunning: isRunning,
            ProcessId: processId,
            AnalysisServicesPort: analysisServicesPort,
            DefaultOutputDirectory: defaultOutputDirectory,
            InstallationType: installType
        ));
    }

    public Task<LaunchProjectResult> LaunchProjectAsync(
        string outputRoot,
        string projectName,
        IDictionary<string, string> reportFiles,
        IDictionary<string, string> semanticModelFiles,
        CancellationToken ct = default)
    {
        var root = string.IsNullOrWhiteSpace(outputRoot) ? GetDefaultOutputDirectory() : outputRoot;
        Directory.CreateDirectory(root);

        var sanitizedProjectName = SanitizeName(projectName);
        _packager.WriteProject(root, sanitizedProjectName, reportFiles, semanticModelFiles);

        var pbipFilePath = Path.GetFullPath(Path.Combine(root, $"{sanitizedProjectName}.pbip"));
        var projectDirectory = Path.GetFullPath(root);

        if (!File.Exists(pbipFilePath))
        {
            return Task.FromResult(new LaunchProjectResult(
                Success: false,
                ProjectName: sanitizedProjectName,
                PbipFilePath: pbipFilePath,
                ProjectDirectory: projectDirectory,
                LaunchedInDesktop: false,
                Message: $"PBIP file could not be generated at '{pbipFilePath}'."
            ));
        }

        var (isInstalled, executablePath, _) = DetectDesktopInstallation();
        var launched = false;
        string? message;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
                {
                    // Launch PBIDesktop directly with the .pbip file
                    var psi = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = $"\"{pbipFilePath}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    launched = true;
                    message = $"Launched project '{sanitizedProjectName}.pbip' in Power BI Desktop.";
                }
                else
                {
                    // Fall back to opening .pbip with default Windows file association
                    var psi = new ProcessStartInfo
                    {
                        FileName = pbipFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    launched = true;
                    message = $"Opened '{sanitizedProjectName}.pbip' via Windows shell file association.";
                }
            }
            else
            {
                message = "Power BI Desktop is only supported natively on Windows.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Power BI Desktop for file: {FilePath}", pbipFilePath);
            message = $"Generated project, but failed to launch Power BI Desktop: {ex.Message}";
        }

        return Task.FromResult(new LaunchProjectResult(
            Success: true,
            ProjectName: sanitizedProjectName,
            PbipFilePath: pbipFilePath,
            ProjectDirectory: projectDirectory,
            LaunchedInDesktop: launched,
            Message: message
        ));
    }

    public bool OpenFolder(string folderPath)
    {
        try
        {
            var target = string.IsNullOrWhiteSpace(folderPath) ? GetDefaultOutputDirectory() : folderPath;
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{Path.GetFullPath(target)}\"",
                    UseShellExecute = true
                });
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder: {FolderPath}", folderPath);
            return false;
        }
    }

    private (bool IsInstalled, string? ExecutablePath, string InstallationType) DetectDesktopInstallation()
    {
        // 1. Check Store / WindowsApps package
        try
        {
            const string windowsApps = @"C:\Program Files\WindowsApps";
            if (Directory.Exists(windowsApps))
            {
                var storeDirs = Directory.GetDirectories(windowsApps, "Microsoft.MicrosoftPowerBIDesktop*");
                foreach (var dir in storeDirs)
                {
                    var exe = Path.Combine(dir, "bin", "PBIDesktop.exe");
                    if (File.Exists(exe))
                    {
                        return (true, exe, "Microsoft Store (WindowsApps)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WindowsApps permission check failed for Power BI Desktop.");
        }

        // 2. Check standard MSI/standalone installer paths
        foreach (var path in PossibleStandalonePaths)
        {
            if (File.Exists(path))
            {
                return (true, path, "Standalone MSI / Setup");
            }
        }

        // 3. Check if running
        var running = Process.GetProcessesByName("PBIDesktop");
        if (running.Length > 0)
        {
            try
            {
                var mainModulePath = running[0].MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(mainModulePath))
                {
                    return (true, mainModulePath, "Active Process");
                }
            }
            catch
            {
                // Access denied for main module inspection is expected on store apps without admin
            }

            return (true, "PBIDesktop.exe", "Active Process");
        }

        return (false, null, "Not Detected");
    }

    private int? TryDetectAnalysisServicesPort(int msmdsrvPid)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"(Get-NetTCPConnection -OwningProcess {msmdsrvPid} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).LocalPort\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);

            if (int.TryParse(output, out var port) && port > 0)
            {
                return port;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query Analysis Services TCP port for PID {Pid}", msmdsrvPid);
        }

        return null;
    }

    private string GetDefaultOutputDirectory()
    {
        var configured = _configuration["PowerBi:LocalOutputDirectory"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        // Default to workspace output/pbip directory
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "output", "pbip"));
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

