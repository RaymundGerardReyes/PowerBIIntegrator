# Data Sources & Schema Extraction API

The Data Sources API ingests relational and flat-file data sources, extracting schemas with mathematical type inference without unbounded memory consumption.

## Endpoints

### 1. Register Data Source
`POST /api/data-sources/register`
- **Description**: Ingests CSV or Excel files in memory-safe streaming batches of 1,000 rows, performing IEEE-754 integer-vs-decimal differentiation, ISO datetime parsing, and sample value collection.
- **Request Body**:
  ```json
  {
    "name": "QuarterlyFinancials",
    "sourceType": "Csv",
    "filePath": "/data/financials_q3.csv"
  }
  ```
- **Response**: `201 Created` (`DataSourceResponse` with registered ID).

### 2. Get Data Source Schema
`GET /api/data-sources/{id}/schema`
- **Description**: Retrieves extracted column definitions, inferred data types, nullability, and top 5 distinct sample values.
- **Response**: `200 OK` (`ColumnSchema[]`).

### 3. Register SQL Server Connection
`POST /api/data-sources/sql`
- **Description**: Connects to SQL Server using dual-strategy extraction (`SqlDataReader.GetSchemaTable(CommandBehavior.SchemaOnly)` primary with `INFORMATION_SCHEMA.COLUMNS` fallback).
- **Response**: `201 Created`.

