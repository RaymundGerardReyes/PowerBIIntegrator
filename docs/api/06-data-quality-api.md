# Data Quality & Transformation Engine (DQTE) API

The DQTE API exposes endpoints to profile, validate, deduplicate, clean, transform, and map chart recommendations for multi-source datasets.

## Endpoints

### 1. Profile Dataset
`POST /api/data-quality/profile`
- **Payload**: `{ "sourceReference": "data/sales.csv", "datasetName": "SalesBatch2026" }`
- **Response**: `DatasetProfile` containing per-column inferred types, null ratios, distinct counts, cardinality classes, and regex signatures.

### 2. Clean Dataset (Bronze → Silver)
`POST /api/data-quality/clean`
- **Payload**: `{ "sourceReference": "data/sales.csv", "datasetName": "SalesBatch2026" }`
- **Response**: `PipelineRunResult` recording rows processed, duplicates dropped, and stage summaries.

### 3. Transform Dataset (Silver → Gold)
`POST /api/data-quality/transform`
- **Payload**: `{ "silverSourceTable": "silver_sales", "transformationPlanName": "FactSalesPlan", "targetGoldTable": "gold_fact_sales" }`
- **Response**: `PipelineRunResult` with Gold table generation metrics.

### 4. Run Full Pipeline
`POST /api/data-quality/run-full-pipeline`
- **Payload**: `{ "sourceReference": "data/sales.csv", "datasetName": "SalesBatch2026", "targetGoldTable": "gold_fact_sales" }`
- **Response**: End-to-end execution summary from Bronze ingestion to Gold materialization.

### 5. Get Chart Suggestions
`GET /api/data-quality/chart-suggestions/{tableId}`
- **Response**: List of heuristic visual suggestions (`lineChart`, `barChart`, `scatterPlot`) ranked with confidence scores and explainable rationale strings.

