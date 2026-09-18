---
name: semantic-model-measure-parity
description: Enforce complete parity between PBIR visual bound fields and TMDL semantic model definitions
trigger: always_on
---

# Invariant
Every column and measure referenced in a PBIR visual (`visual.json` or `boundFields`) MUST exist in the corresponding TMDL semantic model table (`<TableName>.tmdl`).

## Rules
- When generating visual definitions that bind to `TotalRows` or any other measure, ensure that measure is explicitly defined in `model.Tables[n].Measures` with a valid DAX formula (e.g., `measure 'TotalRows' = COUNTROWS('TableName')`).
- KPI Cards and Single-Value visuals must strictly bind to valid DAX measures, never unaggregated raw columns (e.g. use `Total_Revenue` or `TotalRevenue`, NOT `Revenue`).
- In `Visual.cs` and query binding generators, do not classify raw column names (such as `Revenue` or `Amount`) as measures. Only classify names starting with `Total`, `Sum`, `Average`, or ending with `_Rate` as measures.
- In all fallback and default models (e.g. `SalesAnalyticsModel`), always declare `TotalRows` in addition to domain metrics so default visuals render without `Missing_References` errors.
- In `TmdlGenerator`, NEVER output `Source = #table(type table [], {})`. Always generate a realistic `#table(...)` sample partition matching all declared table columns and types when no external partition is provided, ensuring Power BI visuals render with real data rather than `(Blank)` or crashing with zero-column errors.
- In `AnalyticsModelFactory`, embed actual file partition queries (`File.Contents("...")`) for uploaded CSV/Excel files:
  - For Excel (`.xlsx`, `.xls`): Use `Excel.Workbook(File.Contents("..."), null, true)` with `Source{0}[Data]`.
  - For CSV (`.csv`): Use `Csv.Document(File.Contents("..."), [Delimiter=",", Encoding=65001, QuoteStyle=QuoteStyle.None])`.
  - In `Table.TransformColumnTypes`, always format numeric types as `type number` (never bare `number`), `Int64.Type`, `type datetime`, `type logical`, or `type text`.
- In PBIP download and compilation handlers, always look up models by project/dataset name if in-memory ID lookup fails after service restarts.
- Always order semantic models newest-first so newly uploaded datasets become the default active model across dashboard and local orchestrator workspaces.
