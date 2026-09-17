import { http, HttpResponse } from "msw";
import { env } from "@shared/config/env";

export const handlers = [
  http.get(`${env.apiBaseUrl}/api/analytics/models`, async () =>
    HttpResponse.json([
      {
        id: "22222222-2222-2222-2222-222222222222",
        name: "TitanicSurvival2026 Semantic Model",
        tables: [{ name: "TitanicSurvival2026" }]
      }
    ])
  ),
  http.get(`${env.apiBaseUrl}/api/dashboards/model/:modelId`, () =>
    HttpResponse.json({
      id: "88888888-8888-8888-8888-888888888888",
      name: "TitanicSurvival2026 Dashboard",
      pages: [
        {
          name: "Overview & Analytics",
          canvasWidth: 1280,
          canvasHeight: 720,
          visuals: [
            {
              name: "kpi-total-records",
              visualType: "card",
              layout: { x: 40, y: 30, width: 340, height: 160, z: 1, visible: true },
              boundFields: ["TitanicSurvival2026[TotalRows]"]
            },
            {
              name: "chart-pclass",
              visualType: "barChart",
              layout: { x: 40, y: 220, width: 720, height: 440, z: 1, visible: true },
              boundFields: ["TitanicSurvival2026[pclass]", "TitanicSurvival2026[TotalRows]"]
            }
          ]
        }
      ]
    })
  ),
  http.get(`${env.apiBaseUrl}/api/dashboards/:id`, ({ params }) =>
    HttpResponse.json({
      id: params.id,
      name: "TitanicSurvival2026 Dashboard",
      pages: []
    })
  ),
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
  http.get(`${env.apiBaseUrl}/api/powerbi/desktop/status`, () =>
    HttpResponse.json({
      isInstalled: true,
      desktopExecutablePath: "C:\\Program Files\\WindowsApps\\Microsoft.MicrosoftPowerBIDesktop_x64__8wekyb3d8bbwe\\bin\\PBIDesktop.exe",
      isRunning: true,
      processId: 5368,
      analysisServicesPort: 29761,
      defaultOutputDirectory: "D:\\PowerBIEnhanced\\output\\pbip",
      installationType: "Microsoft Store (WindowsApps)"
    })
  ),
  http.post(`${env.apiBaseUrl}/api/powerbi/desktop/launch`, async ({ request }) => {
    const body = (await request.json()) as { projectName?: string };
    const name = body?.projectName || "AnalyticsProject";
    return HttpResponse.json({
      success: true,
      projectName: name,
      pbipFilePath: `D:\\PowerBIEnhanced\\output\\pbip\\${name}.pbip`,
      projectDirectory: `D:\\PowerBIEnhanced\\output\\pbip\\${name}`,
      launchedInDesktop: true,
      message: `Launched project '${name}.pbip' in Power BI Desktop.`
    });
  }),
  http.post(`${env.apiBaseUrl}/api/powerbi/desktop/open-folder`, async () =>
    HttpResponse.json({ success: true })
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
  http.get(`${env.apiBaseUrl}/api/data-sources`, async () =>
    HttpResponse.json([])
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
