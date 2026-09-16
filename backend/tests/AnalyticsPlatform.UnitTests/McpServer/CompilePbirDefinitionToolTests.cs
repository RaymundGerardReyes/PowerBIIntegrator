using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.Application.Features.PowerBiPublishing.Commands.CompilePbirDefinition;
using AnalyticsPlatform.Domain.Common;
using AnalyticsPlatform.McpServer.Tools.PowerBi;
using Xunit;

namespace AnalyticsPlatform.UnitTests.McpServer;

public sealed class CompilePbirDefinitionToolTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task ExecuteAsync_WithValidDashboardId_CallsMediatorAndReturnsSuccess()
    {
        var dashboardId = Guid.NewGuid();
        var files = new Dictionary<string, string>
        {
            ["definition/report.json"] = "{}",
            ["definition/pages/page1.json"] = "{}"
        };

        var compilationResponse = new PbirCompilationResponse("ExecutiveDashboard", 2, files);
        _mediator.Send(Arg.Any<CompilePbirDefinitionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<PbirCompilationResponse>.Success(compilationResponse));

        var tool = new CompilePbirDefinitionTool(_mediator, NullLogger<CompilePbirDefinitionTool>.Instance);
        var inputJson = JsonDocument.Parse($"{{\"dashboardDefinitionId\": \"{dashboardId}\"}}").RootElement;

        var result = await tool.ExecuteAsync(inputJson, "corr-123", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("ExecutiveDashboard");
        result.Content[0].Text.Should().Contain("report.json");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingDashboardId_ReturnsFailureResult()
    {
        var tool = new CompilePbirDefinitionTool(_mediator, NullLogger<CompilePbirDefinitionTool>.Instance);
        var inputJson = JsonDocument.Parse("{}").RootElement;

        var result = await tool.ExecuteAsync(inputJson, "corr-123", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Missing or invalid 'dashboardDefinitionId'");
    }
}
