# Principal Engineering Plan: Rule-Driven Data Quality & Transformation Engine

Owner: Principal Software Engineer / Principal Software Architect mindset
Stack alignment: .NET 10 LTS (Domain/Application/Infrastructure/Api), React 19 + TS 7 frontend, Power BI PBIP/PBIR target
Scope: Deterministic, non-AI engine for profiling, deduplication, cleaning, transformation, and chart suggestion
Status: Planning document — no code merged yet

---

## 1. Problem Statement and Goals

The platform ingests more than six heterogeneous sources (Excel, CSV, SQL) that must be merged into a single canonical analytics model used to drive Power BI, Excel, Word, and PDF outputs. Today, data quality issues — duplicate rows, schema drift, inconsistent formats — are handled ad hoc inside connectors. This is not sustainable as source count and data volume grow.

**Goal:** Design and implement a standalone, rule-driven Data Quality & Transformation Engine (DQTE) that:

- Profiles every incoming dataset and column deterministically (no ML/AI).
- Detects redundant/duplicate rows using explicit, testable algorithms.
- Cleans and standardizes data before it is promoted to analytical tables.
- Transforms cleaned data into target schemas (star-schema fact/dimension tables).
- Recommends chart/visual types for the resulting tables using heuristic mapping rules.
- Is fully covered by the existing six-category test taxonomy (Unit, Integration, Path, Regression, E2E, Security).
- Leaves an explicit extension point for an optional AI/LLM advisory layer without making AI a dependency of core correctness.

**Non-goals:** Statistical/ML-based anomaly detection, LLM-based rule generation, real-time streaming ingestion (batch/micro-batch only in v1).

---

## 2. Guiding Architectural Principles

1. **Determinism first.** Every decision the engine makes (dedupe, drop, transform) must be explainable and reproducible from the same inputs and the same rule configuration — no hidden heuristics, no black boxes.
2. **Rules are data, not code.** Business rules (schema constraints, dedupe keys, cleaning policies) are configuration objects, versioned and stored, not hardcoded `if` statements scattered across services.
3. **Pipeline as Chain of Responsibility.** Each pipeline stage (profile → validate → clean → transform → suggest) is a composable handler; handlers can be added, reordered, or disabled without touching unrelated stages, mirroring the Chain of Responsibility / middleware pattern already used for ASP.NET Core pipelines.[web:107][web:113][web:118]
4. **Medallion staging.** Data moves through Bronze (raw, schema-flexible) → Silver (validated, partially locked) → Gold (fully locked, contract-enforced) layers, consistent with modern schema-evolution guidance.[web:97][web:98]
5. **Fail loud in dev, fail safe in prod.** Rule violations throw in local/dev pipelines for fast feedback; in production they route to a quarantine table and emit structured telemetry rather than crashing the pipeline.
6. **Idempotency and replayability.** Re-running the pipeline on the same source snapshot must produce identical Gold tables — critical for regression testing and safe retries.[web:104][web:91]
7. **No AI in the critical path.** The engine must produce correct, auditable output with zero calls to any LLM or ML model. AI is an optional read-only advisor layered on top, never a gate.
8. **Domain owns rules; Infrastructure owns execution.** Rule definitions and decisions live in the Domain layer as pure C#; only I/O (reading files/SQL, writing tables) lives in Infrastructure — consistent with the existing Clean/Hexagonal boundaries already enforced by NetArchTest.[file:102]

---

## 3. High-Level Architecture

```
                     DATA QUALITY & TRANSFORMATION ENGINE (DQTE)
                                      │
      ┌───────────────┬──────────────┼──────────────┬───────────────┐
      ▼               ▼              ▼               ▼               ▼
  Profiling      Validation       Cleaning       Transformation   Chart
  Stage          Stage            Stage          Stage            Suggestion
  (ColumnProfile) (SchemaRules)   (DedupeRules,  (TransformPlan)  (VisualMapping
                                   RepairRules)                    Rules)
      │               │              │               │               │
      └───────────────┴──────────────┴───────────────┴───────────────┘
                                      │
                             PIPELINE ORCHESTRATOR
                        (Chain of Responsibility, ordered
                         stages, short-circuit on fatal errors)
                                      │
                ┌─────────────────────┼──────────────────────┐
                ▼                     ▼                      ▼
           Bronze Tables         Silver Tables           Gold Tables
           (raw + metadata)   (validated + cleaned)   (transformed, contract-
                                                         locked, chart-ready)
                                      │
                                      ▼
                     Canonical Analytics Model (Domain)
                                      │
                ┌─────────────────────┼──────────────────────┐
                ▼                     ▼                      ▼
             Power BI (PBIR)       Excel/Word/PDF        Optional AI Advisor
                                                          (read-only, MCP tool)
```

This slots directly into the existing repository layout as a new feature: `Features/DataQuality` in Domain, Application, Infrastructure, and Api, alongside the existing `Analytics`, `Dashboards`, `DataSources`, and `PowerBiPublishing` features.[file:102]

---

## 4. Domain Model (pure C#, no external dependencies)

New entities and value objects under `AnalyticsPlatform.Domain/Features/DataQuality`:

| Type | Responsibility |
|---|---|
| `DatasetProfile` | Aggregate profile for one ingested table/file: row count, column profiles, profiling timestamp, source reference. |
| `ColumnProfile` | Per-column stats: inferred type, null ratio, distinct count, min/max, top-N values, detected pattern (regex signature), cardinality class. |
| `SchemaContract` | Versioned expected schema for a Silver/Gold table: required columns, types, nullability, key columns. |
| `SchemaCompatibilityRule` | Rule evaluated against `SchemaContract` vs. incoming `DatasetProfile` (already partially present as `SchemaCompatibilityRules`; extend it). |
| `DedupeRuleSet` | Ordered list of dedupe strategies (`ExactHashRule`, `CompositeKeyRule`, `SimilarityClusterRule`) with thresholds. |
| `DuplicateCluster` | Group of row identifiers considered duplicates, with the winning/kept row and reason code. |
| `CleaningRule` | Deterministic rule: trim/standardize casing, normalize date formats, null-handling policy, outlier clipping policy. |
| `TransformationPlan` | Ordered list of transformation steps (join, aggregate, pivot, rename, derive) from Silver source(s) to a Gold target schema. |
| `TransformationStep` | Single step in a plan; pure function over `TabularBatch` producing another `TabularBatch`. |
| `VisualMappingRule` | Heuristic rule mapping a Gold table's column-role signature (e.g., datetime+numeric) to a recommended visual type. |
| `PipelineRunResult` | Immutable record of what happened in a run: rows in/out per stage, duplicates removed, rules triggered, quarantined rows. |

All of these are plain C# records/entities with domain rule methods (`Result<T> Evaluate(...)`), following the existing `Result.cs` / `DomainException.cs` conventions already established in the codebase.[file:102]

---

## 5. Application Layer: Orchestration Logic

### 5.1 Pipeline Orchestrator (Chain of Responsibility)

Implement `IDataQualityStage` as the common handler interface:

```csharp
public interface IDataQualityStage
{
    Task<StageResult> ExecuteAsync(PipelineContext context, CancellationToken ct);
}
```

Concrete stages, each single-responsibility and independently testable:

1. `ProfilingStage` — builds `DatasetProfile` from a `TabularBatch`.
2. `SchemaValidationStage` — evaluates `SchemaCompatibilityRule`s; can short-circuit (reject) or downgrade to "quarantine" depending on severity.
3. `DeduplicationStage` — applies `DedupeRuleSet` (exact hash → composite key → similarity clustering), removing/marking rows and producing `DuplicateCluster`s.
4. `CleaningStage` — applies `CleaningRule`s (standardize formats, null handling, trimming, outlier policy).
5. `TransformationStage` — executes the target `TransformationPlan`, producing the Gold `TabularBatch`.
6. `ChartSuggestionStage` — inspects Gold schema column roles and applies `VisualMappingRule`s to produce ranked visual suggestions.

`PipelineOrchestrator` composes stages in order, using the Chain of Responsibility / middleware pattern so stages can be added, removed, or reordered via configuration rather than code changes, mirroring proven .NET patterns for validation pipelines.[web:107][web:112][web:118]

```csharp
public sealed class PipelineOrchestrator
{
    private readonly IReadOnlyList<IDataQualityStage> _stages;

    public async Task<PipelineRunResult> RunAsync(
        PipelineContext context, CancellationToken ct)
    {
        foreach (var stage in _stages)
        {
            var result = await stage.ExecuteAsync(context, ct);
            context.Record(result);
            if (result.IsFatal)
                break; // short-circuit, route to quarantine
        }
        return context.BuildRunResult();
    }
}
```

### 5.2 Use Cases (MediatR Commands/Queries)

Add to `Features/DataQuality` in the Application layer, consistent with existing `Commands`/`Queries` structure:[file:102]

- `ProfileDatasetCommand` / Handler — runs Stage 1 only, used for preview/UI feedback before committing.
- `ValidateDatasetSchemaCommand` — runs Stages 1–2, returns pass/fail + violations (already exists as a stub; extend it).
- `CleanDatasetCommand` — runs Stages 1–4, writes Silver table, returns `PipelineRunResult`.
- `TransformDatasetCommand` — runs Stage 5 against a named `TransformationPlan`, writes Gold table.
- `SuggestChartsForTableQuery` — runs Stage 6 against an existing Gold table.
- `RunFullPipelineCommand` — orchestrates Stages 1–6 end-to-end for a given source registration.
- `GetPipelineRunHistoryQuery` — audit/history retrieval for observability.

### 5.3 Cross-Cutting Behaviors

Reuse existing MediatR pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`, `PerformanceBehavior`) and add:

- `PipelineRunAuditBehavior` — persists a `PipelineRunResult` for every DQTE command, feeding the audit/history UI.
- `QuarantineRoutingBehavior` — on fatal validation failures, writes rejected batches to a `Quarantine` table instead of throwing an unhandled exception in production.

---

## 6. Deduplication Design (Deterministic, No ML)

Layered strategy, each layer opt-in via `DedupeRuleSet` configuration:

1. **Exact-hash dedupe** — compute a stable hash (e.g., SHA-256) over a canonicalized row representation (sorted column order, normalized casing/whitespace); rows with identical hash and no business key mismatch are exact duplicates. This is a standard, well-studied technique in structured duplicate detection and scales well for batch jobs.[web:89][web:96]
2. **Composite business-key dedupe** — using `SchemaContract`-declared key columns (e.g., `CustomerId + InvoiceNumber + Date`); on collision, apply a configurable resolution policy: keep latest by timestamp, keep highest completeness score, or merge non-conflicting fields.[web:96]
3. **Similarity-cluster dedupe (optional, still deterministic)** — for free-text columns (names, addresses), apply string-similarity metrics such as Levenshtein or Jaro-Winkler with a fixed threshold, grouping near-duplicates into `DuplicateCluster`s for either automatic merge or manual review, following established duplicate-detection literature (blocking → candidate generation → pairwise comparison → decision).[web:93][web:96][web:102]

Every dedupe decision must record: rule that fired, rows compared, similarity score (if applicable), and the row kept/dropped — required for the Regression test category (golden-file snapshots) and for auditability.

---

## 7. Schema & Cleaning Rule Design

- **Schema contracts are versioned** (`SchemaContract.Version`) and stored alongside migrations; incompatible changes require an explicit new version, not silent mutation — aligned with medallion "gold fully locks with an authoritative contract" guidance.[web:97]
- **Cleaning rules are declarative**, expressed as an ordered list per column or column-pattern (e.g., "all `*_Date` columns → parse and normalize to UTC ISO-8601"; "all `Amount` columns → strip currency symbols, parse decimal, reject on failure").
- **Null/missing-value policy is explicit per field**: reject row, default value, forward-fill, or route to quarantine — never a silent implicit choice.
- **Outlier policy is rule-based** (e.g., clip or flag values outside configured min/max or IQR bounds) — not model-based anomaly detection, to keep the system AI-free by design.

This matches standard ETL data-cleansing practice: removing duplicates, correcting errors, handling missing values, and standardizing formats before data reaches downstream systems.[web:95][web:92][web:98]

---

## 8. Transformation & Chart-Suggestion Design

- `TransformationPlan` is a directed, ordered list of `TransformationStep`s (filter, join, aggregate, pivot, derive-column, rename) executed against Silver tables to produce Gold tables — this mirrors the "transformation" phase of standard ETL architecture (filtering, sorting, aggregating, joining, cleaning, deduplicating, validating).[web:91][web:98]
- Plans are versioned and stored as configuration (JSON/YAML or DB rows), not embedded in code, so a Principal Engineer or data steward can review/approve changes without a deployment.
- `VisualMappingRule`s inspect the **column-role signature** of a Gold table (e.g., one datetime + one numeric → time series; one categorical + one numeric aggregate → bar chart; two numeric → scatter; geo column present → map) and emit ranked `ChartSuggestion` objects consumed by the Dashboard IR compiler already defined for PBIR/Excel/Word/PDF output.[file:102]
- Because this is rule-based and explainable, every suggestion carries a `Reason` string (e.g., "datetime + numeric measure detected → line chart recommended") for UI transparency and for future audit by an optional AI advisor.

---

## 9. Infrastructure Layer

New adapters under `AnalyticsPlatform.Infrastructure/Features/DataQuality`:

- `TabularBatchReader` — unifies Excel/CSV/SQL sources into a common `TabularBatch` abstraction (reuses existing `ExcelDataSourceReader`, `CsvDataSourceReader`, SQL connectors).[file:102]
- `HashDedupeEngine`, `CompositeKeyDedupeEngine`, `SimilarityClusterDedupeEngine` — concrete implementations of the dedupe strategies.
- `SchemaContractRepository` — persistence for versioned schema contracts.
- `PipelineRunRepository` — persistence for `PipelineRunResult` audit history.
- `QuarantineWriter` — writes rejected/failed batches to a dedicated quarantine table with reason codes.

All adapters implement Domain-defined interfaces only (`IDedupeEngine`, `ITabularBatchReader`), preserving the existing dependency-inversion rule enforced by architecture tests.[file:102]

---

## 10. API Layer

New endpoints in `AnalyticsPlatform.Api/Endpoints/DataQualityEndpoints.cs`:

- `POST /data-quality/profile` — preview profiling for an uploaded/selected dataset.
- `POST /data-quality/validate` — schema validation preview.
- `POST /data-quality/clean` — run cleaning, return Silver preview + `PipelineRunResult`.
- `POST /data-quality/transform` — run a named `TransformationPlan`, produce Gold table.
- `GET /data-quality/chart-suggestions/{tableId}` — return ranked chart suggestions.
- `GET /data-quality/runs/{id}` — audit/history detail.

Middleware reuse: `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `RateLimitingMiddleware` already in place apply unchanged.[file:102]

---

## 11. Frontend Integration (React 19 + TS 7)

New feature slice `features/data-quality` mirroring existing `data-sources` and `dashboards` features:[file:102]

- `api/dataQualityApi.ts` — typed calls to the new endpoints (types generated from shared OpenAPI contract).
- `components/ProfileSummaryPanel.tsx` — shows column profiles (types, null %, cardinality, detected patterns).
- `components/DuplicateReviewTable.tsx` — lists `DuplicateCluster`s with kept/dropped rows and reason codes; allows manual override before commit.
- `components/CleaningRuleEditor.tsx` — lets a user configure/adjust cleaning rules per column.
- `components/TransformationPlanBuilder.tsx` — visual builder for `TransformationStep` sequences.
- `components/ChartSuggestionPanel.tsx` — displays ranked chart suggestions with the rule-based `Reason`, feeding into `DashboardCanvas.tsx`.
- `hooks/usePipelineRun.ts` — orchestrates the profile → validate → clean → transform → suggest flow and surfaces progress/errors.

No business logic lives in these components; all decisions come from the backend engine, keeping the frontend a pure presentation layer per existing architectural rules.[file:102]

---

## 12. Testing Strategy (mapped to existing six-category taxonomy)

| Category | Examples specific to DQTE |
|---|---|
| Unit | `HashDedupeEngineTests`, `CompositeKeyDedupeEngineTests`, `SchemaCompatibilityRuleTests`, `CleaningRuleTests`, `VisualMappingRuleTests` — each rule tested in isolation with crafted edge cases (nulls, type mismatches, near-duplicate strings). |
| Integration | `PipelineOrchestratorTests` against a real Testcontainers SQL instance and sample Excel/CSV fixtures; `QuarantineWriterTests` verifying rejected rows are persisted correctly. |
| Path | `MultiSourceIngestionPathTests` (extend existing) — walks 6+ Excel/CSV files plus a SQL source through profile → validate → clean → transform → suggest, asserting final row counts and duplicate counts match expected fixtures. |
| Regression | `DedupeDecisionRegressionTests` and `TransformationOutputRegressionTests` using golden-file snapshots (Verify.Xunit) so silent drift in cleaning/dedupe logic is caught immediately. |
| E2E | `DataCleaningToChartWorkflow.feature` (Reqnroll/SpecFlow) — upload dataset → run full pipeline → verify chart suggestions rendered in UI → verify Gold table published to Power BI. |
| Security | `SchemaInjectionTests` (malicious Excel/CSV payloads), `SqlInjectionTests` for SQL-backed transformation steps, dependency vulnerability scans — reusing existing `AnalyticsPlatform.SecurityTests` project.[file:102] |

Maintain the existing test-pyramid ratio guardrail (unit ≫ integration ≫ path/regression ≫ E2E) enforced via CI test-count reporting.[file:102]

---

## 13. Observability & Governance

- **Structured logging** with feature-tagged scopes (`DataQuality.Profiling`, `DataQuality.Dedupe`, `DataQuality.Transform`) consistent with existing Serilog conventions.[file:102]
- **Correlation IDs** propagated from upload through to the final Gold table and Power BI publish step, so a data steward can trace one file's full journey.[file:102]
- **PipelineRunResult dashboard** in the UI showing rows in/out, duplicates removed, rules triggered, and quarantined counts per run — this is the audit trail required for a "testable" engine.
- **Rule versioning and change log**: every change to `SchemaContract`, `DedupeRuleSet`, `CleaningRule`, or `TransformationPlan` is versioned with an author, timestamp, and diff, enabling rollback — following data-quality-framework governance guidance (model rules, automate monitoring, onboard stewards).[web:117][web:108]
- **Human-in-the-loop for ambiguous cases**: similarity-based duplicate clusters below a confidence threshold are routed to `DuplicateReviewTable.tsx` for manual resolution rather than auto-merged, keeping the system safe by default.[web:108]

---

## 14. Delivery Plan (Phased Rollout)

| Phase | Scope | Exit criteria |
|---|---|---|
| Phase 0 — Foundations | Domain entities (`DatasetProfile`, `ColumnProfile`, `SchemaContract`), `PipelineOrchestrator` skeleton, unit tests for profiling only. | Profiling stage runs on all 6+ source types with passing unit tests. |
| Phase 1 — Validation & Quarantine | `SchemaCompatibilityRule` extensions, `QuarantineWriter`, `SchemaValidationStage`. | Invalid/incompatible sources are rejected or quarantined, never silently accepted. |
| Phase 2 — Deduplication | Exact-hash + composite-key dedupe engines, `DuplicateCluster`, review UI. | Duplicate rows removed deterministically; regression snapshots in place. |
| Phase 3 — Cleaning | `CleaningRule` engine, null/outlier policies, `CleaningRuleEditor.tsx`. | Silver tables consistently standardized; path tests green. |
| Phase 4 — Transformation | `TransformationPlan` executor, Gold table materialization. | Gold tables feed existing PBIR/Excel/Word/PDF generators unchanged. |
| Phase 5 — Chart Suggestion | `VisualMappingRule` engine, `ChartSuggestionPanel.tsx`, integration with Dashboard IR. | Suggested visuals appear correctly in generated dashboards. |
| Phase 6 — Similarity Dedupe (stretch) | Levenshtein/Jaro-Winkler clustering for free-text fields. | Near-duplicate clusters surfaced with confidence scores for manual review. |
| Phase 7 — Optional AI Advisor Hook | Read-only MCP tool exposing `PipelineRunResult` + profiles for LLM-based explanation/suggestions (per prior LLM Gateway design). | AI layer can be disabled entirely with zero impact on core pipeline correctness. |

---

## 15. Risks and Mitigations

- **Risk: rule sprawl becomes unmaintainable.** Mitigate by keeping rules as versioned configuration with a dedicated review UI and change log, not scattered code.[web:117]
- **Risk: similarity-based dedupe produces false merges.** Mitigate with conservative default thresholds, mandatory review queue for low-confidence clusters, and regression snapshots to catch behavior drift.[web:96][web:102]
- **Risk: schema drift silently breaks downstream Power BI/Excel outputs.** Mitigate with the Bronze/Silver/Gold contract-locking strategy and `SchemaValidationStage` short-circuiting on incompatible changes.[web:97]
- **Risk: performance degradation with 6+ large sources.** Mitigate with idempotent, partitionable transformation steps and incremental/CDC-style loads instead of full reloads where possible.[web:98][web:91]
- **Risk: future AI layer accidentally becomes load-bearing.** Mitigate architecturally by keeping the AI advisor strictly read-only and outside the `PipelineOrchestrator` critical path, as planned in Phase 7.

---

## 16. Summary

This plan delivers a fully deterministic, rule-driven Data Quality & Transformation Engine that profiles, validates, deduplicates, cleans, transforms, and recommends charts for arbitrary multi-source datasets (6+ Excel/CSV/SQL sources), using proven, explainable techniques — hash-based and key-based deduplication, versioned schema contracts, declarative cleaning rules, and heuristic chart-mapping rules — rather than AI/ML.[web:89][web:91][web:95][web:96][web:97][web:98] It slots cleanly into the existing Clean/Hexagonal `.NET 10` backend and Feature-Sliced React 19/TS 7 frontend, reuses the established six-category test taxonomy, and leaves a clearly bounded, optional extension point for a future read-only LLM/MCP advisory layer without ever making correctness dependent on AI.[file:102]
