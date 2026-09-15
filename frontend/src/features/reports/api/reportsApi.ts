import { apiClient } from "@shared/lib/http/apiClient";

export async function fetchReportBlob(reportId: string, format: "pdf" | "xlsx" | "docx"): Promise<Blob> {
  const { data } = await apiClient.get(`/api/reports/${reportId}/${format}`, { responseType: "blob" });
  return data;
}
