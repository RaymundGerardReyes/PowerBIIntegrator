using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AnalyticsPlatform.Application.Features.ReportGeneration.Contracts;
using AnalyticsPlatform.Application.Features.ReportGeneration.Models;

namespace AnalyticsPlatform.Infrastructure.DocumentGenerators.Pdf;

public sealed class PdfReportGenerator : IPdfReportGenerator
{
    static PdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateAsync(ReportDocumentModel model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeHeader(header, model));
                page.Content().Element(content => ComposeContent(content, model));
                page.Footer().Element(footer => ComposeFooter(footer, model));
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static void ComposeHeader(IContainer container, ReportDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(model.Title)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    if (!string.IsNullOrWhiteSpace(model.Subtitle))
                    {
                        col.Item().Text(model.Subtitle)
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(120).AlignRight().Column(col =>
                {
                    col.Item().Text(model.Organization)
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);

                    var genDate = model.GeneratedAt ?? DateTimeOffset.UtcNow;
                    col.Item().Text(genDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                        .FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, ReportDocumentModel model)
    {
        container.PaddingVertical(16).Column(column =>
        {
            if (model.Sections == null || model.Sections.Count == 0)
            {
                column.Item().Text("No sections available in this report.").Italic().FontColor(Colors.Grey.Medium);
                return;
            }

            foreach (var section in model.Sections)
            {
                column.Item().PaddingBottom(16).Column(secCol =>
                {
                    secCol.Item().Text(section.Title)
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Blue.Darken1);

                    if (!string.IsNullOrWhiteSpace(section.Narrative))
                    {
                        secCol.Item().PaddingTop(4).Text(section.Narrative)
                            .FontSize(10)
                            .LineHeight(1.2f);
                    }

                    // KPI Cards via Table layout
                    if (section.Kpis != null && section.Kpis.Count > 0)
                    {
                        secCol.Item().PaddingTop(8).Table(kpiTable =>
                        {
                            kpiTable.ColumnsDefinition(cols =>
                            {
                                int colCount = Math.Min(section.Kpis.Count, 3);
                                for (int i = 0; i < colCount; i++)
                                    cols.RelativeColumn();
                            });

                            foreach (var kpi in section.Kpis)
                            {
                                kpiTable.Cell()
                                    .Padding(4)
                                    .Background(Colors.Grey.Lighten4)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(8)
                                    .Column(kpiCol =>
                                    {
                                        kpiCol.Item().Text(kpi.Label)
                                            .FontSize(8)
                                            .FontColor(Colors.Grey.Darken1);

                                        kpiCol.Item().Text(kpi.Value)
                                            .FontSize(16)
                                            .Bold()
                                            .FontColor(Colors.Blue.Darken3);

                                        if (!string.IsNullOrWhiteSpace(kpi.DeltaPercentage))
                                        {
                                            var isPositive = !kpi.DeltaPercentage.StartsWith('-');
                                            kpiCol.Item().Text(kpi.DeltaPercentage)
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor(isPositive ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                        }

                                        if (!string.IsNullOrWhiteSpace(kpi.Description))
                                        {
                                            kpiCol.Item().Text(kpi.Description)
                                                .FontSize(7)
                                                .FontColor(Colors.Grey.Medium);
                                        }
                                    });
                            }
                        });
                    }

                    // Data Table
                    if (section.TableHeaders != null && section.TableHeaders.Count > 0)
                    {
                        secCol.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < section.TableHeaders.Count; i++)
                                {
                                    columns.RelativeColumn();
                                }
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                foreach (var h in section.TableHeaders)
                                {
                                    header.Cell()
                                        .Background(Colors.Blue.Darken2)
                                        .Padding(6)
                                        .Text(h)
                                        .FontSize(9)
                                        .Bold()
                                        .FontColor(Colors.White);
                                }
                            });

                            // Data Rows
                            if (section.TableRows != null)
                            {
                                for (int r = 0; r < section.TableRows.Count; r++)
                                {
                                    var rowData = section.TableRows[r];
                                    var bg = r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                    for (int c = 0; c < section.TableHeaders.Count; c++)
                                    {
                                        var cellText = c < rowData.Count ? rowData[c] : string.Empty;
                                        table.Cell()
                                            .Background(bg)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(cellText)
                                            .FontSize(8);
                                    }
                                }
                            }
                        });
                    }
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container, ReportDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Confidential — {model.Organization} (v{model.Author})")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                });
            });
        });
    }
}
