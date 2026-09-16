# ADR 0005: Rule-Driven Data Quality & Transformation Engine (DQTE)

## Status
Accepted

## Context
The platform ingests heterogeneous multi-source datasets (Excel, CSV, SQL) that must be merged into canonical analytics models feeding Power BI (PBIR/PBIP), Excel, Word, and PDF report outputs. Previously, data quality checks were handled in an ad-hoc manner inside connectors, leading to silent failures, schema drift, and duplicate rows.

## Decision
We implement a standalone, rule-driven Data Quality & Transformation Engine (DQTE) adhering to the following architectural invariants:
1. **Determinism First (No AI in Critical Path)**: Profiling, deduplication, schema compatibility, cleaning, and chart suggestions are 100% deterministic and rule-driven. AI/LLM models are strictly restricted to an optional read-only advisory layer via MCP tools.
2. **Chain of Responsibility Orchestrator**: The pipeline is partitioned into independent, single-responsibility stages (`ProfilingStage`, `SchemaValidationStage`, `DeduplicationStage`, `CleaningStage`, `TransformationStage`, `ChartSuggestionStage`) orchestrated sequentially with short-circuiting on fatal errors.
3. **Medallion Architecture**: Data progresses deterministically from Bronze (raw ingested tabular data) → Silver (validated, standardized, deduplicated) → Gold (contract-locked, transformed, chart-ready star schema).
4. **Fail Loud in Dev, Fail Safe in Prod**: In development, schema violations fail fast. In production, failing records are routed to `Quarantine` storage with structured reason codes.

## Consequences
- **Positive**: 100% reproducible and auditable pipelines; transparent deduplication decisions (exact hash, composite keys, similarity clusters); robust regression snapshots.
- **Trade-off**: Requires explicit configuration of schema contracts and transformation plans rather than black-box statistical inference.

