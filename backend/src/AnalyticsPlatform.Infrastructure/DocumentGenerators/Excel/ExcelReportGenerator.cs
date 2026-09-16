using System.Globalization;
using ClosedXML.Excel;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;
using AnalyticsPlatform.Domain.Features.ReportGeneration.Rules;

namespace AnalyticsPlatform.Infrastructure.DocumentGenerators.Excel;

public sealed class ExcelReportGenerator : IExcelReportGenerator
{
    public Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();

        // 1. Executive Summary Worksheet
        var summaryWs = workbook.Worksheets.Add("Executive Summary");
        summaryWs.ShowGridLines = true;

        // Title Banner
        summaryWs.Cell("A1").Value = model.Title;
        summaryWs.Cell("A1").Style.Font.Bold = true;
        summaryWs.Cell("A1").Style.Font.FontSize = 18;
        summaryWs.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0078D4");

        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            summaryWs.Cell("A2").Value = model.Subtitle;
            summaryWs.Cell("A2").Style.Font.Italic = true;
            summaryWs.Cell("A2").Style.Font.FontSize = 11;
            summaryWs.Cell("A2").Style.Font.FontColor = XLColor.Gray;
        }

        // Metadata block
        summaryWs.Cell("A4").Value = "Organization:";
        summaryWs.Cell("A4").Style.Font.Bold = true;
        summaryWs.Cell("B4").Value = model.Organization;

        summaryWs.Cell("A5").Value = "Author:";
        summaryWs.Cell("A5").Style.Font.Bold = true;
        summaryWs.Cell("B5").Value = model.Author;

        summaryWs.Cell("A6").Value = "Generated At:";
        summaryWs.Cell("A6").Style.Font.Bold = true;
        summaryWs.Cell("B6").Value = (model.GeneratedAt ?? DateTimeOffset.UtcNow).ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);

        int currentRow = 8;

        if (model.Sections != null && model.Sections.Count > 0)
        {
            foreach (var section in model.Sections)
            {
                // Section Header
                summaryWs.Cell(currentRow, 1).Value = section.Title;
                summaryWs.Cell(currentRow, 1).Style.Font.Bold = true;
                summaryWs.Cell(currentRow, 1).Style.Font.FontSize = 14;
                summaryWs.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#106EBE");
                summaryWs.Range(currentRow, 1, currentRow, 6).Merge();
                summaryWs.Range(currentRow, 1, currentRow, 6).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                summaryWs.Range(currentRow, 1, currentRow, 6).Style.Border.BottomBorderColor = XLColor.FromHtml("#0078D4");
                currentRow += 2;

                if (!string.IsNullOrWhiteSpace(section.Narrative))
                {
                    summaryWs.Cell(currentRow, 1).Value = section.Narrative;
                    summaryWs.Cell(currentRow, 1).Style.Font.FontSize = 10;
                    summaryWs.Range(currentRow, 1, currentRow, 6).Merge();
                    summaryWs.Range(currentRow, 1, currentRow, 6).Style.Alignment.WrapText = true;
                    currentRow += 2;
                }

                // KPIs
                if (section.Kpis != null && section.Kpis.Count > 0)
                {
                    int kpiCol = 1;
                    int kpiStartRow = currentRow;

                    foreach (var kpi in section.Kpis)
                    {
                        var kpiRange = summaryWs.Range(kpiStartRow, kpiCol, kpiStartRow + 2, kpiCol + 1);
                        kpiRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        kpiRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D2D0CE");
                        kpiRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F2F1");

                        summaryWs.Cell(kpiStartRow, kpiCol).Value = kpi.Label;
                        summaryWs.Cell(kpiStartRow, kpiCol).Style.Font.FontSize = 9;
                        summaryWs.Cell(kpiStartRow, kpiCol).Style.Font.FontColor = XLColor.DarkGray;

                        summaryWs.Cell(kpiStartRow + 1, kpiCol).Value = kpi.Value;
                        summaryWs.Cell(kpiStartRow + 1, kpiCol).Style.Font.Bold = true;
                        summaryWs.Cell(kpiStartRow + 1, kpiCol).Style.Font.FontSize = 14;
                        summaryWs.Cell(kpiStartRow + 1, kpiCol).Style.Font.FontColor = XLColor.FromHtml("#004E8C");

                        if (!string.IsNullOrWhiteSpace(kpi.DeltaPercentage))
                        {
                            summaryWs.Cell(kpiStartRow + 2, kpiCol).Value = kpi.DeltaPercentage;
                            summaryWs.Cell(kpiStartRow + 2, kpiCol).Style.Font.Bold = true;
                            summaryWs.Cell(kpiStartRow + 2, kpiCol).Style.Font.FontSize = 9;
                            summaryWs.Cell(kpiStartRow + 2, kpiCol).Style.Font.FontColor = kpi.DeltaPercentage.StartsWith('-')
                                ? XLColor.FromHtml("#A80000")
                                : XLColor.FromHtml("#107C10");
                        }

                        kpiCol += 2;
                        if (kpiCol > 5)
                        {
                            kpiCol = 1;
                            kpiStartRow += 4;
                        }
                    }

                    currentRow = kpiStartRow + 4;
                }
            }
        }

        summaryWs.Columns().AdjustToContents(1, 20);

        // 2. Data Tables Worksheet (if sections have tables)
        if (model.Sections != null && model.Sections.Any(s => s.TableHeaders != null && s.TableHeaders.Count > 0))
        {
            var dataWs = workbook.Worksheets.Add("Data Tables");
            dataWs.ShowGridLines = true;
            int tableRow = 1;

            foreach (var section in model.Sections.Where(s => s.TableHeaders != null && s.TableHeaders.Count > 0))
            {
                dataWs.Cell(tableRow, 1).Value = section.Title;
                dataWs.Cell(tableRow, 1).Style.Font.Bold = true;
                dataWs.Cell(tableRow, 1).Style.Font.FontSize = 13;
                dataWs.Cell(tableRow, 1).Style.Font.FontColor = XLColor.FromHtml("#0078D4");
                tableRow += 2;

                // Headers
                for (int h = 0; h < section.TableHeaders!.Count; h++)
                {
                    var cell = dataWs.Cell(tableRow, h + 1);
                    cell.Value = section.TableHeaders[h];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D4");
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#004E8C");
                }
                tableRow++;

                // Data Rows
                if (section.TableRows != null)
                {
                    for (int r = 0; r < section.TableRows.Count; r++)
                    {
                        var row = section.TableRows[r];
                        var isEven = r % 2 == 0;

                        for (int c = 0; c < section.TableHeaders.Count; c++)
                        {
                            var cell = dataWs.Cell(tableRow, c + 1);
                            var rawVal = c < row.Count ? row[c] : string.Empty;
                            var sanitized = ReportGenerationRules.SanitizeCellForFormulaInjection(rawVal);

                            // Check numeric parse
                            if (decimal.TryParse(rawVal, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal) && !sanitized.StartsWith('\''))
                            {
                                cell.Value = decVal;
                            }
                            else
                            {
                                cell.Value = sanitized;
                            }

                            cell.Style.Fill.BackgroundColor = isEven ? XLColor.White : XLColor.FromHtml("#F3F2F1");
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#EDEBE9");
                        }
                        tableRow++;
                    }
                }

                tableRow += 2; // Spacing between tables
            }

            dataWs.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
