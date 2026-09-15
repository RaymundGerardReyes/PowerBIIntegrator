using FluentAssertions;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Infrastructure.PowerBi;
using Xunit;

namespace AnalyticsPlatform.SecurityTests;

public class PbipPathTraversalSecurityTests
{
    [Fact]
    public void VirtualFileTree_WriteToDirectory_WhenPathContainsTraversal_ThrowsInvalidOperationException()
    {
        var tree = new VirtualFileTree();
        tree.AddTextFile("../../../traversal.txt", "malicious payload");

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var act = () => tree.WriteToDirectory(tempDir);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*traversal*");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void PbipPackager_WhenRelativePathEscapesRoot_ThrowsInvalidOperationException()
    {
        var packager = new PbipPackager();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var maliciousReportFiles = new Dictionary<string, string>
        {
            ["../../../../../../escape.txt"] = "payload"
        };

        try
        {
            var act = () => packager.WriteProject(tempDir, "SafeProject", maliciousReportFiles, new Dictionary<string, string>());
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*traversal*");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void PbipCompiler_WithMaliciousProjectName_SanitizesFileNameSafely()
    {
        var pbirGen = new PbirGenerator();
        var tmdlGen = new TmdlGenerator();
        var compiler = new PbipCompiler(pbirGen, tmdlGen);

        var dashboard = new DashboardDefinition("TestDashboard", Guid.NewGuid());
        var model = new AnalyticsModel("TestModel");

        var tree = compiler.CompileProject("../../../Malicious/Project", dashboard, model);

        // Project descriptor should be sanitized
        tree.ContainsFile("MaliciousProject.pbip").Should().BeTrue();
        tree.Files.Any(f => f.RelativePath.Contains("../")).Should().BeFalse();
    }
}

