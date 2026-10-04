using FluentAssertions;
using MediatR;
using NSubstitute;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Application.Features.AiAdvisory.Interfaces;
using AnalyticsPlatform.Application.Features.DataQuality.Abstractions;
using AnalyticsPlatform.Application.Features.DataQuality.Commands;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration;
using AnalyticsPlatform.Application.Features.DataQuality.Orchestration.Stages;
using AnalyticsPlatform.Domain.Features.DataSources.Entities;
using AnalyticsPlatform.Domain.Repositories;
using AnalyticsPlatform.Infrastructure.Features.AiAdvisory.Repositories;
using AnalyticsPlatform.Infrastructure.Repositories;
using Xunit;

namespace AnalyticsPlatform.UnitTests.Application.DataQuality;

public class PipelineCommandHandlersTests
{
    [Fact]
    public async Task RunFullPipelineCommandHandler_ExecutesOrchestratorAndEmbedsMQueryPartition()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"full_pipeline_{Guid.NewGuid()}.csv");
        try
        {
            File.WriteAllText(tempFile, "EmpId,Name,Cost\n001,  John  ,₱500\n002,Mary,$750\n001,  John  ,₱500\n");

            var dsRepo = Substitute.For<IDataSourceRepository>();
            var readerFactory = Substitute.For<IDataSourceReaderFactory>();
            var reader = Substitute.For<IDataSourceReader>();

            var rawRows = new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["EmpId"] = "001", ["Name"] = "  John  ", ["Cost"] = "₱500" },
                new Dictionary<string, object?> { ["EmpId"] = "002", ["Name"] = "Mary", ["Cost"] = "$750" },
                new Dictionary<string, object?> { ["EmpId"] = "001", ["Name"] = "  John  ", ["Cost"] = "₱500" } // Duplicate
            };

            reader.ReadAsync(tempFile, Arg.Any<CancellationToken>()).Returns(rawRows);
            readerFactory.GetReader(DataSourceType.Csv).Returns(reader);

            var advisoryRepo = new InMemoryAdvisoryRunRepository();
            var sender = Substitute.For<ISender>();
            var modelRepo = new AnalyticsModelRepository();

            var stages = new IDataQualityStage[]
            {
                new ProfilingStage(readerFactory),
                new DeduplicationStage(),
                new CleaningStage(),
                new TransformationStage(modelRepo)
            };
            var orchestrator = new PipelineOrchestrator(stages);

            var handler = new RunFullPipelineCommandHandler(
                dsRepo,
                readerFactory,
                advisoryRepo,
                sender,
                modelRepo,
                orchestrator);

            var command = new RunFullPipelineCommand(tempFile, "EmployeesDataset", "GoldEmployees");

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            var run = result.Value!;

            // Verify stages ran through PipelineOrchestrator
            run.StageSummaries.Select(s => s.StageName).Should().Contain(new[] { "Profiling", "Deduplication", "Cleaning", "Transformation", "ChartSuggestion" });

            // Deduplication quarantined 1 duplicate row
            var dedupeStage = run.StageSummaries.First(s => s.StageName == "Deduplication");
            dedupeStage.InputRowCount.Should().Be(3);
            dedupeStage.OutputRowCount.Should().Be(2);
            dedupeStage.QuarantinedRowCount.Should().Be(1);

            // Verify Gold model was stored with M Query partition
            var allModels = await modelRepo.GetAllAsync();
            allModels.Should().ContainSingle(m => m.Tables.Any(t => t.Name == "GoldEmployees"));
            var goldTable = allModels.First(m => m.Tables.Any(t => t.Name == "GoldEmployees")).Tables.First(t => t.Name == "GoldEmployees");
            goldTable.MQueryPartition.Should().NotBeNull();
            goldTable.MQueryPartition.Should().Contain("Csv.Document(File.Contents(");
            goldTable.MQueryPartition.Should().Contain("#\"Cleaned Text\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CleanDatasetCommandHandler_ExecutesStagesThroughOrchestrator()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"clean_pipeline_{Guid.NewGuid()}.csv");
        try
        {
            File.WriteAllText(tempFile, "Product,Amount\nWidget,₱1,000\nWidget,₱1,000\nGadget,$500\n");

            var dsRepo = Substitute.For<IDataSourceRepository>();
            var readerFactory = Substitute.For<IDataSourceReaderFactory>();
            var reader = Substitute.For<IDataSourceReader>();

            var rawRows = new List<IDictionary<string, object?>>
            {
                new Dictionary<string, object?> { ["Product"] = "Widget", ["Amount"] = "₱1,000" },
                new Dictionary<string, object?> { ["Product"] = "Widget", ["Amount"] = "₱1,000" },
                new Dictionary<string, object?> { ["Product"] = "Gadget", ["Amount"] = "$500" }
            };

            reader.ReadAsync(tempFile, Arg.Any<CancellationToken>()).Returns(rawRows);
            readerFactory.GetReader(DataSourceType.Csv).Returns(reader);

            var stages = new IDataQualityStage[]
            {
                new ProfilingStage(readerFactory),
                new DeduplicationStage(),
                new CleaningStage()
            };
            var orchestrator = new PipelineOrchestrator(stages);

            var handler = new CleanDatasetCommandHandler(
                dsRepo,
                readerFactory,
                orchestrator);

            var command = new CleanDatasetCommand(tempFile, "SalesDataset");

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            var run = result.Value!;

            run.StageSummaries.Select(s => s.StageName).Should().ContainInOrder("Profiling", "Deduplication", "Cleaning");
            var dedupe = run.StageSummaries.First(s => s.StageName == "Deduplication");
            dedupe.InputRowCount.Should().Be(3);
            dedupe.OutputRowCount.Should().Be(2);
            dedupe.QuarantinedRowCount.Should().Be(1);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
