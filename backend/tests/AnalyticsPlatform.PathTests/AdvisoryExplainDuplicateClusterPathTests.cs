using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AnalyticsPlatform.Application.Features.AiAdvisory.Commands.RunAdvisoryQuery;
using AnalyticsPlatform.Application.Features.AiAdvisory.Common;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Application.Features.AiAdvisory.Services;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Audit;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Repositories;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Synthesizer;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.ToolRegistry;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Tools;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public sealed class AdvisoryExplainDuplicateClusterPathTests
{
    private readonly RunAdvisoryQueryCommandHandler _handler;

    public AdvisoryExplainDuplicateClusterPathTests()
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
        var assembler = new AdvisoryContextAssembler(registry);
        var synthesizer = new GroundedAdvisorySynthesizer();
        var auditLogger = new AdvisoryAuditLogger(NullLogger<AdvisoryAuditLogger>.Instance);

        _handler = new RunAdvisoryQueryCommandHandler(
            assembler,
            synthesizer,
            auditLogger,
            NullLogger<RunAdvisoryQueryCommandHandler>.Instance);
    }

    [Fact]
    public async Task FullAdvisoryPath_ExplainDuplicateCluster_ReturnsGroundedCitations()
    {
        // Arrange
        var request = new AdvisoryQueryRequest(
            RunId: "run-demo-001",
            QuestionType: "Duplicates",
            UserQuestion: "Why were these 42 rows treated as duplicates?",
            UserRole: "Analyst");

        // Act
        var result = await _handler.Handle(new RunAdvisoryQueryCommand(request), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Should().NotBeNull();
        dto!.RunId.Should().Be("run-demo-001");
        dto.Answer.Should().Contain("42 rows were routed to quarantine");
        dto.Answer.Should().Contain("ExactHashRule-v2");
        dto.CitedRuleIds.Should().Contain("ExactHashRule-v2");
        dto.CitedRuleIds.Should().Contain("CompositeKeyRule-CustInv");
        dto.CitedRunIds.Should().Contain("run-demo-001");
        dto.ProviderUsed.Should().Be("LocalOllama"); // Default privacy-preserving provider
    }

    [Fact]
    public async Task FullAdvisoryPath_OutsideScopeQuestion_ReturnsExplicitScopeBoundaryMessage()
    {
        // Arrange
        var request = new AdvisoryQueryRequest(
            RunId: "run-demo-001",
            QuestionType: "General",
            UserQuestion: "Why is my revenue lower this month?");

        // Act
        var result = await _handler.Handle(new RunAdvisoryQueryCommand(request), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto!.Answer.Should().Contain("outside advisory scope");
    }
}

