import { http, HttpResponse } from "msw";
import { env } from "@shared/config/env";

export const handlers = [
  http.post(`${env.apiBaseUrl}/api/analytics/measures`, async () => HttpResponse.json({ id: "measure-1" })),
  http.post(`${env.apiBaseUrl}/api/analytics/models/:id/validate`, ({ params }) =>
    HttpResponse.json({
      modelId: params.id,
      modelName: "SalesModel",
      isValid: true,
      errors: [],
      warnings: [],
      orphanTables: [],
      detectedCycles: []
    })
  ),
  http.get(`${env.apiBaseUrl}/api/powerbi/embed-config/:reportId`, ({ params }) =>
    HttpResponse.json({ reportId: params.reportId, embedUrl: "https://app.powerbi.com/embed", accessToken: "fake-token" })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/compile-pbir`, async () =>
    HttpResponse.json({ reportId: "report-1", files: { "definition/report.json": "{}" } })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/compile-tmdl`, async () =>
    HttpResponse.json({ modelName: "SalesModel", tables: [{ name: "Sales" }], relationships: [] })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/compile-pbip`, async () =>
    HttpResponse.json({
      projectName: "TestProject",
      pbirDefinition: { reportId: "report-1", files: {} },
      tmdlModel: { modelName: "TestModel" }
    })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/compile-pbip/download`, async () =>
    new HttpResponse(new Blob(["PK\x03\x04fakezip"], { type: "application/zip" }), {
      headers: { "Content-Type": "application/zip" }
    })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/import`, async () =>
    HttpResponse.json({
      importId: "import-1",
      datasetId: "ds-1",
      displayName: "ImportedDataset",
      importState: "Succeeded"
    })
  ),
  http.post(`${env.apiBaseUrl}/api/data-sources/upload`, async () =>
    HttpResponse.json({ id: "ds-1", name: "sales.xlsx", type: "excel", connectionOrPath: "/tmp/sales.xlsx" })
  ),
  http.get(`${env.apiBaseUrl}/api/data-sources/schema/:id`, ({ params }) =>
    HttpResponse.json({
      dataSourceId: params.id,
      dataSourceName: "Sales Database",
      tables: [{ name: "Orders", columns: [{ name: "Id", dataType: "int" }, { name: "Amount", dataType: "decimal" }] }]
    })
  ),
  http.post(`${env.apiBaseUrl}/api/reports/pdf`, async () =>
    new HttpResponse(new Blob(["%PDF-1.4 fake pdf"], { type: "application/pdf" }), {
      headers: { "Content-Type": "application/pdf" }
    })
  ),
  http.post(`${env.apiBaseUrl}/api/reports/excel`, async () =>
    new HttpResponse(new Blob(["fake excel"], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }), {
      headers: { "Content-Type": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
    })
  ),
  http.post(`${env.apiBaseUrl}/api/reports/word`, async () =>
    new HttpResponse(new Blob(["fake docx"], { type: "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }), {
      headers: { "Content-Type": "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
    })
  )
];
