using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using AnalyticsPlatform.McpServer.Tools;
using Xunit;

namespace AnalyticsPlatform.UnitTests.McpServer;

public sealed class McpToolRegistryTests
{
    [Fact]
    public void RegisterTool_AddsToolToRegistry()
    {
        var mockTool = Substitute.For<IMcpTool>();
        mockTool.Name.Returns("test_tool");
        mockTool.Description.Returns("A test tool description");
        mockTool.InputSchemaJson.Returns("{}");
        mockTool.OutputSchemaJson.Returns("{}");

        var registry = new ToolRegistry(new[] { mockTool }, NullLogger<ToolRegistry>.Instance);

        registry.GetAllTools().Should().ContainSingle(t => t.Name == "test_tool");
        registry.TryGetTool("test_tool", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(mockTool);
    }

    [Fact]
    public async Task ExecuteToolAsync_WhenToolNotFound_ReturnsErrorResult()
    {
        var registry = new ToolRegistry(Enumerable.Empty<IMcpTool>(), NullLogger<ToolRegistry>.Instance);
        var inputDoc = JsonDocument.Parse("{}");

        var result = await registry.ExecuteToolAsync("non_existent_tool", inputDoc.RootElement, Guid.NewGuid().ToString(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Content.Should().ContainSingle(c => c.Text.Contains("not registered"));
    }

    [Fact]
    public async Task ExecuteToolAsync_WhenToolExists_DelegatesExecution()
    {
        var mockTool = Substitute.For<IMcpTool>();
        mockTool.Name.Returns("echo_tool");
        mockTool.ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(McpToolExecutionResult.SuccessText("Echo response"));

        var registry = new ToolRegistry(new[] { mockTool }, NullLogger<ToolRegistry>.Instance);
        var inputDoc = JsonDocument.Parse("{}");

        var result = await registry.ExecuteToolAsync("echo_tool", inputDoc.RootElement, "test-corr-id", CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Be("Echo response");
    }
}

