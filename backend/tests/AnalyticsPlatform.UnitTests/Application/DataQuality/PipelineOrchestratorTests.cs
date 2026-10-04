using FluentAssertions;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Domain.Features.DataQuality.Entities;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.DataQuality;

public class PipelineOrchestratorTests
{
    private sealed class TestStage : IDataQualityStage
    {
        public string StageName { get; }
        private readonly bool _isSuccess;
        private readonly bool _isFatal;

        public TestStage(string stageName, bool isSuccess = true, bool isFatal = false)
        {
            StageName = stageName;
            _isSuccess = isSuccess;
            _isFatal = isFatal;
        }

        public Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct)
        {
            context.DataBatches[StageName] = "executed";
            return Task.FromResult(new StageResult(
                StageName,
                _isSuccess,
                _isFatal,
                100,
                100,
                0,
                new[] { $"{StageName}Rule" },
                $"{StageName} summary"
            ));
        }
    }

    [Fact]
    public async Task RunAsync_ExecutesAllStagesInOrder()
    {
        var stage1 = new TestStage("Stage1");
        var stage2 = new TestStage("Stage2");
        var orchestrator = new PipelineOrchestrator(new[] { stage1, stage2 });

        var context = new PipelineContext("run-1", "source.csv", new Dictionary<string, object>());

        var result = await orchestrator.RunAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StageSummaries.Should().HaveCount(2);
        result.StageSummaries[0].StageName.Should().Be("Stage1");
        result.StageSummaries[1].StageName.Should().Be("Stage2");
        context.DataBatches.Should().ContainKey("Stage1");
        context.DataBatches.Should().ContainKey("Stage2");
    }

    [Fact]
    public async Task RunAsync_ShortCircuitsOnFatalError()
    {
        var stage1 = new TestStage("FatalStage", isSuccess: false, isFatal: true);
        var stage2 = new TestStage("UnreachedStage");
        var orchestrator = new PipelineOrchestrator(new[] { stage1, stage2 });

        var context = new PipelineContext("run-2", "source.csv", new Dictionary<string, object>());

        var result = await orchestrator.RunAsync(context, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StageSummaries.Should().HaveCount(1);
        result.StageSummaries[0].StageName.Should().Be("FatalStage");
        context.DataBatches.Should().NotContainKey("UnreachedStage");
    }

    [Fact]
    public async Task RunStagesAsync_ExecutesOnlySpecifiedStages()
    {
        var stage1 = new TestStage("Stage1");
        var stage2 = new TestStage("Stage2");
        var stage3 = new TestStage("Stage3");
        var orchestrator = new PipelineOrchestrator(new[] { stage1, stage2, stage3 });

        var context = new PipelineContext("run-3", "source.csv", new Dictionary<string, object>());

        var result = await orchestrator.RunStagesAsync(context, new[] { "Stage1", "Stage3" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StageSummaries.Should().HaveCount(2);
        result.StageSummaries.Select(s => s.StageName).Should().ContainInOrder("Stage1", "Stage3");
        context.DataBatches.Should().ContainKey("Stage1");
        context.DataBatches.Should().NotContainKey("Stage2");
        context.DataBatches.Should().ContainKey("Stage3");
    }

    [Fact]
    public async Task RunStagesAsync_PreservesRequestedStageOrderEvenIfDifferentFromRegistrationOrder()
    {
        var stage1 = new TestStage("Alpha");
        var stage2 = new TestStage("Beta");
        var orchestrator = new PipelineOrchestrator(new[] { stage1, stage2 });

        var context = new PipelineContext("run-order", "source.csv", new Dictionary<string, object>());

        // Caller asks for Beta, then Alpha
        var result = await orchestrator.RunStagesAsync(context, new[] { "Beta", "Alpha" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StageSummaries.Select(s => s.StageName).Should().ContainInOrder("Beta", "Alpha");
    }
}
