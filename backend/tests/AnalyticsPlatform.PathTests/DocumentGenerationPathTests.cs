using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateExcelReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GeneratePdfReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Commands.GenerateWordReport;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Domain.Features.Analytics.Entities;
using AnalyticsPlatform.Domain.Features.Analytics.ValueObjects;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Excel;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Pdf;
using AnalyticsPlatform.Infrastructure.DocumentGenerators.Word;
using FluentAssertions;
using Xunit;

namespace AnalyticsPlatform.PathTests;

public class DocumentGenerationPathTests
{
    [Fact]
    public async Task AnalyticsModel_ToMultiTargetDocumentGeneration_ExecutesFullBusinessPath()
    {
        // 1. Construct Domain Analytics Model
        var analyticsModel = new AnalyticsModel("EnterpriseSalesModel");

        var salesTable = new ModelTable("Sales");
        salesTable.AddColumn(new ModelColumn("TransactionId", ColumnDataType.Int64, "TransactionId"));
        salesTable.AddColumn(new ModelColumn("Revenue", ColumnDataType.Decimal, "Revenue"));
        salesTable.AddColumn(new ModelColumn("Region", ColumnDataType.String, "Region"));
        analyticsModel.AddTable(salesTable);

        var totalSales = Measure.Create("TotalSales", new MeasureExpression("SUM(Sales[Revenue])", "decimal"), "Sales").Value!;
        analyticsModel.AddMeasure(totalSales);

        // 2. Transform Analytics Model into canonical Report Document Model
        var reportModel = new ReportDocumentModel(
            Title: $"{analyticsModel.Name} Executive Report",
            Subtitle: "Automated Multi-Target Rendering",
            Author: "Analytics Core",
            Organization: "Contoso Enterprise",
            Sections: new List<ReportSectionDto>
            {
                new(
                    Title: "Dataset Schema Summary",
                    Narrative: $"Analytics model contains {analyticsModel.Tables.Count} tables and {analyticsModel.Measures.Count} measures.",
                    Kpis: new List<ReportKpiDto>
                    {
                        new("Table Count", analyticsModel.Tables.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), null, "Registered tables"),
                        new("Measure Count", analyticsModel.Measures.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), null, "Calculated measures")
                    },
                    TableHeaders: new List<string> { "Measure Name", "Expression", "Table" },
                    TableRows: analyticsModel.Measures.Select(m => (IReadOnlyList<string>)new List<string>
                    {
                        m.Name,
                        m.Expression.DaxOrFormula,
                        m.TableName
                    }).ToList())
            });

        // 3. Instantiate Generators & Handlers
        var pdfGen = new PdfReportGenerator();
        var excelGen = new ExcelReportGenerator();
        var wordGen = new WordReportGenerator();

        var pdfHandler = new GeneratePdfReportCommandHandler(pdfGen);
        var excelHandler = new GenerateExcelReportCommandHandler(excelGen);
        var wordHandler = new GenerateWordReportCommandHandler(wordGen);

        // 4. Execute PDF Generation
        var pdfResult = await pdfHandler.Handle(new GeneratePdfReportCommand(reportModel), CancellationToken.None);
        pdfResult.IsSuccess.Should().BeTrue();
        pdfResult.Value.Should().NotBeNull();
        pdfResult.Value!.ContentType.Should().Be("application/pdf");
        pdfResult.Value.Content.Length.Should().BeGreaterThan(1000);

        // 5. Execute Excel Generation
        var excelResult = await excelHandler.Handle(new GenerateExcelReportCommand(reportModel), CancellationToken.None);
        excelResult.IsSuccess.Should().BeTrue();
        excelResult.Value.Should().NotBeNull();
        excelResult.Value!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        excelResult.Value.Content.Length.Should().BeGreaterThan(1000);

        // 6. Execute Word Generation
        var wordResult = await wordHandler.Handle(new GenerateWordReportCommand(reportModel), CancellationToken.None);
        wordResult.IsSuccess.Should().BeTrue();
        wordResult.Value.Should().NotBeNull();
        wordResult.Value!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        wordResult.Value.Content.Length.Should().BeGreaterThan(1000);
    }
}
