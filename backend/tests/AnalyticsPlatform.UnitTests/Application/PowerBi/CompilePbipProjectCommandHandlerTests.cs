using FluentAssertions;
using NSubstitute;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbipProject;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;
using AnalyticsPlatform.Domain.Repositories;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.PowerBi;

public class CompilePbipProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessfulCompilationResponse()
    {
        var compiler = Substitute.For<IPbipCompiler>();
        var dashboardRepo = Substitute.For<IDashboardRepository>();
        var modelRepo = Substitute.For<IAnalyticsModelRepository>();

        var dashboardId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var dashboard = new DashboardDefinition("TestDashboard", dashboardId);
        var model = new AnalyticsModel("TestModel");

        dashboardRepo.GetByIdAsync(dashboardId, Arg.Any<CancellationToken>()).Returns(dashboard);
        modelRepo.GetByIdAsync(modelId, Arg.Any<CancellationToken>()).Returns(model);

        var mockTree = Substitute.For<IVirtualFileTree>();
        mockTree.Files.Returns(new List<VirtualFile>
        {
            new("TestProject.pbip", "{}"),
            new("TestProject.Report/definition.pbir", "{}"),
            new("TestProject.SemanticModel/definition.pbism", "{}")
        });

        compiler.CompileProject(Arg.Any<string>(), dashboard, model).Returns(mockTree);

        var handler = new CompilePbipProjectCommandHandler(compiler, dashboardRepo, modelRepo);
        var command = new CompilePbipProjectCommand(dashboardId, modelId, "TestProject");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProjectName.Should().Be("TestProject");
        result.Value.TotalFiles.Should().Be(3);
    }
}

