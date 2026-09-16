using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Infrastructure.DocumentGenerators.Word;

public sealed class WordReportGenerator : IWordReportGenerator
{
    public Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var ms = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Section 1: Title
            var titlePara = body.AppendChild(new Paragraph());
            var titleRun = titlePara.AppendChild(new Run());
            var titleProps = titleRun.AppendChild(new RunProperties());
            titleProps.AppendChild(new Bold());
            titleProps.AppendChild(new FontSize { Val = "36" }); // 18pt
            titleProps.AppendChild(new Color { Val = "0078D4" });
            titleProps.AppendChild(new RunFonts { Ascii = "Arial" });
            titleRun.AppendChild(new Text(model.Title));

            // Section 2: Subtitle
            if (!string.IsNullOrWhiteSpace(model.Subtitle))
            {
                var subtitlePara = body.AppendChild(new Paragraph());
                var subtitleRun = subtitlePara.AppendChild(new Run());
                var subProps = subtitleRun.AppendChild(new RunProperties());
                subProps.AppendChild(new Italic());
                subProps.AppendChild(new FontSize { Val = "22" }); // 11pt
                subProps.AppendChild(new Color { Val = "605E5C" });
                subProps.AppendChild(new RunFonts { Ascii = "Arial" });
                subtitleRun.AppendChild(new Text(model.Subtitle));
            }

            // Section 3: Metadata Callout Box
            var metaPara = body.AppendChild(new Paragraph());
            var metaProps = metaPara.AppendChild(new ParagraphProperties());
            var pBrd = metaProps.AppendChild(new ParagraphBorders());
            pBrd.AppendChild(new LeftBorder { Val = BorderValues.Single, Size = 18, Space = 4, Color = "0078D4" });
            metaProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F3F2F1" });

            var metaRun = metaPara.AppendChild(new Run());
            var mrp = metaRun.AppendChild(new RunProperties());
            mrp.AppendChild(new FontSize { Val = "18" }); // 9pt
            mrp.AppendChild(new Color { Val = "323130" });
            mrp.AppendChild(new RunFonts { Ascii = "Arial" });

            var genDate = (model.GeneratedAt ?? DateTimeOffset.UtcNow).ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);
            metaRun.AppendChild(new Text($"Organization: {model.Organization}  |  Author: {model.Author}  |  Date: {genDate}"));

            // Spacing
            body.AppendChild(new Paragraph());

            // Section 4: Document Sections
            if (model.Sections != null && model.Sections.Count > 0)
            {
                foreach (var section in model.Sections)
                {
                    // Section Heading
                    var h1Para = body.AppendChild(new Paragraph());
                    var h1Run = h1Para.AppendChild(new Run());
                    var h1Props = h1Run.AppendChild(new RunProperties());
                    h1Props.AppendChild(new Bold());
                    h1Props.AppendChild(new FontSize { Val = "28" }); // 14pt
                    h1Props.AppendChild(new Color { Val = "106EBE" });
                    h1Props.AppendChild(new RunFonts { Ascii = "Arial" });
                    h1Run.AppendChild(new Text(section.Title));

                    // Section Narrative
                    if (!string.IsNullOrWhiteSpace(section.Narrative))
                    {
                        var nPara = body.AppendChild(new Paragraph());
                        var nRun = nPara.AppendChild(new Run());
                        var nProps = nRun.AppendChild(new RunProperties());
                        nProps.AppendChild(new FontSize { Val = "20" }); // 10pt
                        nProps.AppendChild(new RunFonts { Ascii = "Arial" });
                        nRun.AppendChild(new Text(section.Narrative));
                    }

                    // KPI Metrics Summary
                    if (section.Kpis != null && section.Kpis.Count > 0)
                    {
                        var kpiTable = body.AppendChild(new Table());
                        var tblPr = kpiTable.AppendChild(new TableProperties());
                        tblPr.AppendChild(new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4, Color = "D2D0CE" },
                            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "D2D0CE" },
                            new LeftBorder { Val = BorderValues.None },
                            new RightBorder { Val = BorderValues.None },
                            new InsideHorizontalBorder { Val = BorderValues.None },
                            new InsideVerticalBorder { Val = BorderValues.None }));

                        var kpiRow = kpiTable.AppendChild(new TableRow());
                        foreach (var kpi in section.Kpis)
                        {
                            var cell = kpiRow.AppendChild(new TableCell());
                            var cellPr = cell.AppendChild(new TableCellProperties());
                            cellPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F8F8F8" });

                            var p1 = cell.AppendChild(new Paragraph());
                            var r1 = p1.AppendChild(new Run(new Text(kpi.Label)));
                            r1.RunProperties = new RunProperties(new FontSize { Val = "16" }, new Color { Val = "605E5C" });

                            var p2 = cell.AppendChild(new Paragraph());
                            var r2 = p2.AppendChild(new Run(new Text(kpi.Value)));
                            r2.RunProperties = new RunProperties(new Bold(), new FontSize { Val = "26" }, new Color { Val = "004E8C" });

                            if (!string.IsNullOrWhiteSpace(kpi.DeltaPercentage))
                            {
                                var p3 = cell.AppendChild(new Paragraph());
                                var r3 = p3.AppendChild(new Run(new Text(kpi.DeltaPercentage)));
                                var colorVal = kpi.DeltaPercentage.StartsWith('-') ? "A80000" : "107C10";
                                r3.RunProperties = new RunProperties(new Bold(), new FontSize { Val = "18" }, new Color { Val = colorVal });
                            }
                        }

                        body.AppendChild(new Paragraph());
                    }

                    // Data Table
                    if (section.TableHeaders != null && section.TableHeaders.Count > 0)
                    {
                        var table = body.AppendChild(new Table());
                        var tblPr = table.AppendChild(new TableProperties());
                        tblPr.AppendChild(new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 6, Color = "0078D4" },
                            new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "0078D4" },
                            new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "D2D0CE" },
                            new RightBorder { Val = BorderValues.Single, Size = 4, Color = "D2D0CE" },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "EDEBE9" },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "EDEBE9" }));

                        // Header row
                        var headerRow = table.AppendChild(new TableRow());
                        foreach (var h in section.TableHeaders)
                        {
                            var hCell = headerRow.AppendChild(new TableCell());
                            var cellPr = hCell.AppendChild(new TableCellProperties());
                            cellPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "0078D4" });

                            var hp = hCell.AppendChild(new Paragraph());
                            var hr = hp.AppendChild(new Run(new Text(h)));
                            hr.RunProperties = new RunProperties(new Bold(), new FontSize { Val = "18" }, new Color { Val = "FFFFFF" });
                        }

                        // Data rows
                        if (section.TableRows != null)
                        {
                            for (int r = 0; r < section.TableRows.Count; r++)
                            {
                                var rowData = section.TableRows[r];
                                var dRow = table.AppendChild(new TableRow());
                                var fill = r % 2 == 0 ? "FFFFFF" : "F3F2F1";

                                for (int c = 0; c < section.TableHeaders.Count; c++)
                                {
                                    var cellText = c < rowData.Count ? rowData[c] : string.Empty;
                                    var dCell = dRow.AppendChild(new TableCell());
                                    var cellPr = dCell.AppendChild(new TableCellProperties());
                                    cellPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });

                                    var dp = dCell.AppendChild(new Paragraph());
                                    var dr = dp.AppendChild(new Run(new Text(cellText)));
                                    dr.RunProperties = new RunProperties(new FontSize { Val = "18" }, new Color { Val = "323130" });
                                }
                            }
                        }

                        body.AppendChild(new Paragraph());
                    }
                }
            }

            mainPart.Document.Save();
        }

        return Task.FromResult(ms.ToArray());
    }
}
