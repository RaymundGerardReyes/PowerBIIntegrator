# Invariant
Data cleaning and transformation pipelines must follow Microsoft's systematic Power Query ETL workflow and 3-Layer Architecture across all ingestion, profiling, cleaning, and model generation stages.

## Rules

### 1. The 3-Layer Data Cleaning Model
All dataset cleaning pipelines must execute through three distinct conceptual layers:
- **Layer 1 — Technical Cleaning**:
  - Profile 100% of rows (never truncate to 1,000 rows during profiling) to compute complete null ratios, distinct counts, and cardinality classifications.
  - Strictly normalize numeric values by stripping prefix and suffix currency symbols (`₱`, `$`, `€`, `£`, `¥`), commas, and handling accounting parentheses (e.g. `(₱500)` $\to$ `-500`).
  - Standardize missing values: convert textual representations (`"null"`, `"N/A"`, `"(blank)"`, `"-"`, `"#N/A"`) to true nulls.
  - Clean text fields by trimming leading/trailing whitespace and stripping unprintable ASCII control characters (equivalent to Power Query `Text.Clean`).
- **Layer 2 — Structural Cleaning**:
  - Promote headers (`Table.PromoteHeaders`) and strip non-table metadata rows.
  - Enforce clean alphanumeric identifiers via `SanitizeIdentifier` (removing file extensions, illegal characters, and leading digits).
  - Preserve source column lineage in TMDL (`sourceColumn: <original_name>`).
  - In Gold model materialization, always attach an active Power Query M file partition (`File.Contents(...)`) rather than falling back to mock `#table(...)` definitions.
- **Layer 3 — Business Validation**:
  - Deduplicate based on explicit composite business keys (e.g. `EmployeeID + System`) to prevent deleting legitimate multi-record transactions.
  - Ensure records with all-empty key fields do not collide as duplicates.
  - Maintain 100% measure parity in TMDL: bind KPI cards to valid aggregated DAX measures (`TotalRows`, `Total_<Metric>`, `Average_<Metric>`, `<Metric>_Rate`), never unaggregated raw columns.

### 2. Power Query M "Applied Steps" Narrative
Generated Power Query M partitions must tell a clear sequential story conforming to Power BI Desktop's engine expectations:
```powerquery
let
    Source = Csv.Document(File.Contents("..."), [Delimiter=",", Encoding=65001, QuoteStyle=QuoteStyle.None]),
    #"Promoted Headers" = Table.PromoteHeaders(Source, [PromoteAllScalars=true]),
    #"Changed Type" = Table.TransformColumnTypes(#"Promoted Headers", {{"Cost", type number}, ...}),
    #"Cleaned Text" = Table.TransformColumns(#"Changed Type", {{"Department", Text.Trim, type text}})
in
    #"Cleaned Text"
```
- Numeric types in `Table.TransformColumnTypes` must strictly use `"type number"` (never bare `number`), `Int64.Type`, `type datetime`, `type logical`, or `type text`.

### 3. Pipeline Orchestration & Stage Execution
- The data quality pipeline must execute sequentially through `PipelineOrchestrator` using concrete stages (`ProfilingStage`, `DeduplicationStage`, `CleaningStage`, `TransformationStage`).
- Stages must produce verifiable `StageRunSummary` metrics tracking `InputRows`, `OutputRows`, `QuarantinedRows`, and `RulesFired`.
