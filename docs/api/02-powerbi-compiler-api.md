# Power BI PBIP & TMDL Compiler API

This API compiles canonical C# Intermediate Representation (IR) analytics models into valid Power BI Enhanced Report Format (`PBIR`), Tabular Model Definition Language (`TMDL`), and complete `.pbip` project zip archives.

## Endpoints

### 1. Compile PBIP Project Manifest
`POST /api/powerbi/compile-pbip`
- **Description**: Generates the root `.pbip` JSON manifest and folder structure without creating a physical archive.
- **Request Body**:
  ```json
  {
    "modelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "projectName": "ExecutiveSalesOverview"
  }
  ```
- **Response**: `200 OK` (JSON virtual file manifest).

### 2. Compile & Download PBIP ZIP Archive
`POST /api/powerbi/compile-pbip/download`
- **Description**: Compiles PBIR report definitions and TMDL semantic models in memory and streams a `.pbip.zip` archive.
- **Response**: `200 OK` (`application/zip`).

### 3. Compile PBIR Report Definition
`POST /api/powerbi/compile-pbir`
- **Description**: Generates modular 2026 PBIR files (`definition.pbir`, `report.json`, `pages.json`, per-page `page.json`, and per-visual `visual.json`).
- **Response**: `200 OK` (`IVirtualFileTree` JSON representation).

### 4. Compile TMDL Semantic Model
`POST /api/powerbi/compile-tmdl`
- **Description**: Generates TMDL script hierarchy (`model.tmdl`, `relationships.tmdl`, `tables/*.tmdl`).
- **Response**: `200 OK` (TMDL file paths and script contents).

### 5. Publish to Fabric Workspace
`POST /api/powerbi/publish`
- **Description**: Pushes compiled definitions directly to Microsoft Fabric REST APIs.
- **Response**: `200 OK` with artifact IDs and deployment state.

### 6. Multipart Artifact Direct Import
`POST /api/powerbi/import`
- **Description**: Direct streaming upload of `.pbix`, `.xlsx`, `.rdl`, or `.json` artifacts via `multipart/form-data`.
- **Response**: `200 OK` (`ImportResponseDto`).

