# ADR 0005: Power BI Custom Visuals SDK Capability Mapping Matrix

**Status:** Approved  
**Date:** 2026-09-16  
**Context:** Feasibility Document Line 83 Requirement  
> *"Define a capability matrix that maps the framework's visual types to native Power BI visuals or to custom visuals built with the Power BI Visuals SDK."*

---

## 1. Visual Type Capability Matrix

| Framework Visual Type | Target Implementation | Power BI Visual Name / GUID | Data Roles (`roles`) | Key Formatting Capabilities (`objects`) | PBIR `visual.json` Schema |
|---|---|---|---|---|---|
| `barChart` | Native Power BI | `barChart` / `columnChart` | `Category` (Grouping), `Y` (Measure), `Tooltips` | `dataPoint` (fill, fillRule), `categoryAxis`, `valueAxis` | Native Visual Container |
| `lineChart` | Native Power BI | `lineChart` | `Category` (Timeline), `Y` (Series), `Series` | `dataPoint` (stroke, lineStyle), `legend`, `forecast` | Native Visual Container |
| `card` | Native Power BI | `card` (or new `cardVisual`) | `Fields` (Scalar Measure) | `calloutValue` (font, size, color), `categoryLabel` | Native Visual Container |
| `table` | Native Power BI | `tableEx` | `Values` (Columns & Measures) | `grid`, `columnHeaders`, `values`, `total` | Native Visual Container |
| `matrix` | Native Power BI | `pivotTable` | `Rows`, `Columns`, `Values` | `subTotals`, `grandTotals`, `steppedLayout` | Native Visual Container |
| `donutChart` | Native Power BI | `donutChart` | `Category`, `Y` (Slice Measure) | `legend`, `dataPoint`, `labels` | Native Visual Container |
| `scatterChart` | Native Power BI | `scatterChart` | `X`, `Y`, `Size`, `Details` | `dataPoint` (markerShape, size), `fill` | Native Visual Container |
| `treemap` | Native Power BI | `treeMap` | `Group`, `Details`, `Values` | `dataPoint` (colorPalette), `legend` | Native Visual Container |
| `sankeyDiagram` | Custom Visual (SDK) | `SankeyDiagram1446463273187` | `Source`, `Destination`, `Weight` | `nodeColor`, `linkCurvature`, `dataLabels` | Custom Extension Visual |
| `ganttChart` | Custom Visual (SDK) | `Gantt1448688115699` | `Task`, `StartDate`, `Duration`, `Resource` | `daysOff`, `taskColors`, `legend` | Custom Extension Visual |
| `customD3Visual` | Bespoke Visual (SDK) | `analyticsPlatformD3Visual` | `Dimensions`, `Metrics`, `Grouping` | `customColors`, `animationDuration`, `scaleMode` | Custom Extension Visual |

---

## 2. Power BI Visuals SDK Data Role Mapping (`capabilities.json`)

For visuals compiled with the Power BI Visuals SDK, `capabilities.json` specifies declarative data bindings:

```json
{
  "dataRoles": [
    {
      "displayName": "Source Entity",
      "name": "Source",
      "kind": "Grouping"
    },
    {
      "displayName": "Destination Entity",
      "name": "Destination",
      "kind": "Grouping"
    },
    {
      "displayName": "Flow Metric",
      "name": "Weight",
      "kind": "Measure"
    }
  ],
  "dataViewMappings": [
    {
      "categorical": {
        "categories": {
          "for": { "in": "Source" }
        },
        "values": {
          "select": [
            { "bind": { "to": "Destination" } },
            { "bind": { "to": "Weight" } }
          ]
        }
      }
    }
  ]
}
```

---

## 3. PBIR Compilation Transform (`PbirGenerator`)

When `PbirGenerator` encounters a visual type:
1. **Native Visuals:** Directly emitted in `visual.json` using `"visualType": "barChart"`, `"query": { ... }`, and `"objects": { ... }`.
2. **Custom Visual SDK Visuals:** Emitted with the custom extension identifier and the packaged GUID in `definition/visuals/{visualId}/visual.json`:
   ```json
   {
     "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.4.0/schema.json",
     "name": "visual-sankey-001",
     "visual": {
       "visualType": "SankeyDiagram1446463273187",
       "query": {
         "queryState": {
           "Source": { "projections": ["FactFlows.SourceId"] },
           "Destination": { "projections": ["FactFlows.DestId"] },
           "Weight": { "projections": ["FactFlows.TotalVolume"] }
         }
       }
     }
   }
   ```

