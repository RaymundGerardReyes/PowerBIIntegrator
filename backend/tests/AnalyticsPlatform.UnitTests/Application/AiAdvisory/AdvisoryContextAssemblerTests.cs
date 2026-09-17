using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Application.Features.AiAdvisory.Services;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Repositories;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.ToolRegistry;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.AiAdvisory;

public class AdvisoryContextAssemblerTests
{
    private readonly AdvisoryContextAssembler _assembler;

    public AdvisoryContextAssemblerTests()
    {
        var repo = new InMemoryAdvisoryRunRepository();
        var tools = new IAdvisoryTool[]
        {
            new GetPipelineRunResultTool(repo),
            new GetDatasetProfileTool(repo),
            new GetDuplicateClustersTool(repo),
            new GetSchemaViolationsTool(repo),
            new GetTransformationPlanTool(repo),
            new GetChartSuggestionsTool(repo)
        };

        var registry = new AdvisoryToolRegistry(tools);
        _assembler = new AdvisoryContextAssembler(registry);
    }

    [Fact]
    public async Task AssembleAsync_AssemblesStructuredJson_WithVerifiedRuleIds()
    {
        // Arrange
        var request = new AdvisoryQueryRequest(
            RunId: "run-demo-001",
            QuestionType: "Duplicates",
            UserQuestion: "Why were rows duplicated?");

        // Act
        var context = await _assembler.AssembleAsync(request);

        // Assert
        Assert.NotNull(context);
        Assert.Contains("run-demo-001", context.FormattedContextJson);
        Assert.Contains("ExactHashRule-v2", context.AllowlistedRuleIds);
        Assert.True(context.AllowlistedRuleIds.Count > 0);
        Assert.True(context.RedactedFieldCount >= 0);
    }

    [Fact]
    public async Task AssembleAsync_WithDataStewardUnlock_AllowsSensitiveRows()
    {
        // Arrange
        var request = new AdvisoryQueryRequest(
            RunId: "run-demo-001",
            QuestionType: "Duplicates",
            UserQuestion: "Show me details on duplicate clusters",
            UserRole: "DataSteward",
            UnlockConfidential: true);

        // Act
        var context = await _assembler.AssembleAsync(request);

        // Assert
        Assert.Equal("DataStewardElevated", context.ResolvedPolicy.PolicyName);
        Assert.False(context.ResolvedPolicy.AllowCloudProvider); // Forces LocalOllama
    }
}

