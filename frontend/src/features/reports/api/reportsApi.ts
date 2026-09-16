import { apiClient } from "@shared/lib/http/apiClient";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

export type ReportFormat = "pdf" | "excel" | "word";

export async function generateReportBlob(
  format: ReportFormat,
  model: ReportDocumentModel
): Promise<Blob> {
  const endpoint = `/api/reports/${format}`;
  const { data } = await apiClient.post<Blob>(endpoint, model, {
    responseType: "blob"
  });
  return data;
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  window.URL.revokeObjectURL(url);
}

export async function fetchReportBlob(
  reportIdOrFormat: string,
  format?: "pdf" | "xlsx" | "docx" | "excel" | "word",
  fallbackModel?: ReportDocumentModel
): Promise<Blob> {
  const rawFormat = (format ?? reportIdOrFormat).toLowerCase();
  const normalizedFormat: ReportFormat =
    rawFormat === "xlsx" || rawFormat === "excel"
      ? "excel"
      : rawFormat === "docx" || rawFormat === "word"
        ? "word"
        : "pdf";

  const endpoint = `/api/reports/${normalizedFormat}`;
  const payload: ReportDocumentModel = fallbackModel ?? {
    title: reportIdOrFormat ? `Report ${reportIdOrFormat}` : "Analytics Executive Report",
    subtitle: "Consolidated enterprise KPI & performance overview",
    author: "Analytics Platform",
    organization: "Enterprise Analytics",
    sections: [
      {
        heading: "Executive Summary",
        narrative: "Generated multi-target report from canonical analytics model.",
        kpis: [
          { title: "Total Revenue", value: "$1,250,000", subtitle: "vs prior month", deltaPercent: 8.4, isPositiveDelta: true }
        ],
        tableHeaders: ["Category", "Actual", "Target", "Status"],
        tableRows: [
          ["Enterprise", "$820,000", "$750,000", "Exceeded"],
          ["Commercial", "$430,000", "$400,000", "Exceeded"]
        ]
      }
    ]
  };

  const { data } = await apiClient.post<Blob>(endpoint, payload, {
    responseType: "blob"
  });
  return data;
}
