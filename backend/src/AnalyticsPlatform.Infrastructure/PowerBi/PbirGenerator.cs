using System.Text.Json;
using AnalyticsPlatform.Application.Common.Interfaces;
using AnalyticsPlatform.Domain.Features.Dashboards.Entities;

namespace AnalyticsPlatform.Infrastructure.PowerBi;

public class PbirGenerator : IPbirGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IVirtualFileTree GenerateReportDefinition(DashboardDefinition dashboard, string semanticModelRelativePath = "../SemanticModel")
    {
        var fileTree = new VirtualFileTree();

        // 1. definition.pbir
        var definitionPbir = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json",
            ["version"] = "4.0",
            ["datasetReference"] = new { byPath = new { path = semanticModelRelativePath } }
        };
        fileTree.AddTextFile("definition.pbir", JsonSerializer.Serialize(definitionPbir, JsonOptions));

        // 2. definition/version.json (Mandatory for Power BI Desktop PBIR ExplorationSerializer)
        var versionMetadata = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json",
            ["version"] = "2.0.0"
        };
        fileTree.AddTextFile("definition/version.json", JsonSerializer.Serialize(versionMetadata, JsonOptions));

        // 3. definition/report.json
        var pagesToGenerate = dashboard.Pages.Count > 0 ? dashboard.Pages : new List<Page> { new Page("Page1", 1280, 720) };
        var pageOrder = pagesToGenerate.Select(p => SanitizePageName(p.Name)).ToList();
        var activePage = pageOrder.First();

        var reportJson = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json",
            ["themeCollection"] = new
            {
                baseTheme = new
                {
                    name = "CY24SU02",
                    reportVersionAtImport = "5.56",
                    type = "SharedResources"
                }
            },
            ["layoutOptimization"] = "None"
        };
        fileTree.AddTextFile("definition/report.json", JsonSerializer.Serialize(reportJson, JsonOptions));

        // 4. definition/pages/pages.json
        var pagesMetadata = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json",
            ["pageOrder"] = pageOrder,
            ["activePageName"] = activePage
        };
        fileTree.AddTextFile("definition/pages/pages.json", JsonSerializer.Serialize(pagesMetadata, JsonOptions));

        // 5. Each page and its visuals
        foreach (var page in pagesToGenerate)
        {
            var pageIdentifier = SanitizePageName(page.Name);
            var pageDir = $"definition/pages/{pageIdentifier}";
            
            // page.json (strictly compliant with PBIR page schema)
            var pageJson = new Dictionary<string, object>
            {
                ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.1.0/schema.json",
                ["name"] = pageIdentifier,
                ["displayName"] = page.Name,
                ["displayOption"] = "FitToPage",
                ["width"] = page.CanvasWidth,
                ["height"] = page.CanvasHeight
            };
            fileTree.AddTextFile($"{pageDir}/page.json", JsonSerializer.Serialize(pageJson, JsonOptions));

            // visuals/<visual>/visual.json
            foreach (var visual in page.Visuals)
            {
                var visualPath = $"{pageDir}/visuals/{visual.Name}/visual.json";
                var visualJson = BuildVisualDefinitionJson(visual);
                fileTree.AddTextFile(visualPath, visualJson);
            }
        }

        return fileTree;
    }

    private static string SanitizePageName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Page1";

        var chars = name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        var sanitized = new string(chars);
        return string.IsNullOrWhiteSpace(sanitized) ? "Page1" : sanitized;
    }

    private static string BuildVisualDefinitionJson(Visual visual)
    {
        var categoryProjections = new List<object>();
        var valueProjections = new List<object>();

        if (visual.QueryBinding != null)
        {
            foreach (var cat in visual.QueryBinding.Categories)
            {
                categoryProjections.Add(new
                {
                    field = new
                    {
                        Column = new
                        {
                            Expression = new { SourceRef = new { Entity = cat.Table } },
                            Property = cat.Field
                        }
                    },
                    queryRef = cat.ComputedQueryRef
                });
            }

            foreach (var val in visual.QueryBinding.Values)
            {
                var fieldObj = val.IsMeasure
                    ? (object)new { Measure = new { Expression = new { SourceRef = new { Entity = val.Table } }, Property = val.Field } }
                    : new { Column = new { Expression = new { SourceRef = new { Entity = val.Table } }, Property = val.Field } };

                valueProjections.Add(new
                {
                    field = fieldObj,
                    queryRef = val.ComputedQueryRef
                });
            }
        }

        var visualContainer = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.2.0/schema.json",
            ["name"] = visual.Name,
            ["position"] = new
            {
                x = visual.Layout.X,
                y = visual.Layout.Y,
                width = visual.Layout.Width,
                height = visual.Layout.Height,
                z = visual.Layout.ZOrder
            },
            ["visual"] = new
            {
                visualType = visual.VisualType,
                query = new
                {
                    queryState = new Dictionary<string, object>
                    {
                        ["Category"] = new { projections = categoryProjections },
                        ["Y"] = new { projections = valueProjections }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(visualContainer, JsonOptions);
    }

    public string GeneratePageJson(Page page)
    {
        var pageDefinition = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.1.0/schema.json",
            ["name"] = page.Name,
            ["displayName"] = page.Name,
            ["displayOption"] = "FitToPage",
            ["width"] = page.CanvasWidth,
            ["height"] = page.CanvasHeight,
            ["visualContainers"] = page.Visuals.Select(v => new
            {
                name = v.Name,
                visualType = v.VisualType,
                x = v.Layout.X,
                y = v.Layout.Y,
                width = v.Layout.Width,
                height = v.Layout.Height,
                z = v.Layout.ZOrder,
                visible = v.Layout.Visible,
                fields = v.BoundFields
            })
        };

        return JsonSerializer.Serialize(pageDefinition, JsonOptions);
    }

    public string GenerateDefinitionPbir(string semanticModelRelativePath)
    {
        var definition = new Dictionary<string, object>
        {
            ["$schema"] = "https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json",
            ["version"] = "4.0",
            ["datasetReference"] = new { byPath = new { path = semanticModelRelativePath } }
        };

        return JsonSerializer.Serialize(definition, JsonOptions);
    }
}
