import { http, HttpResponse } from "msw";
import { env } from "@shared/config/env";

export const handlers = [
  http.post(`${env.apiBaseUrl}/api/analytics/measures`, async () => HttpResponse.json({ id: "measure-1" })),
  http.get(`${env.apiBaseUrl}/api/powerbi/embed-config/:reportId`, ({ params }) =>
    HttpResponse.json({ reportId: params.reportId, embedUrl: "https://app.powerbi.com/embed", accessToken: "fake-token" })
  ),
  http.post(`${env.apiBaseUrl}/api/data-sources/upload`, async () => HttpResponse.json({ id: "ds-1", name: "sales.xlsx", type: "excel", connectionOrPath: "/tmp/sales.xlsx" }))
];
