# Multi-Target Report Generation API

The Report Generation API compiles canonical `ReportDocumentModel` specifications into high-fidelity executive documents across three targets: vector PDF, spreadsheet Excel, and formatted Word documents.

## Endpoints

### 1. Generate PDF Report
`POST /api/reports/pdf`
- **Engine**: QuestPDF vector layout engine.
- **Request Body**: `ReportDocumentModel`
- **Response**: `200 OK` (`application/pdf`) starting with binary magic bytes `%PDF-`.

### 2. Generate Excel Spreadsheet
`POST /api/reports/excel`
- **Engine**: ClosedXML with automatic formula injection escaping (`=`, `+`, `-`, `@`).
- **Request Body**: `ReportDocumentModel`
- **Response**: `200 OK` (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`).

### 3. Generate Word Document
`POST /api/reports/word`
- **Engine**: OpenXml WordprocessingML generator.
- **Request Body**: `ReportDocumentModel`
- **Response**: `200 OK` (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`).

