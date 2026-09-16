import { useQuery } from "@tanstack/react-query";
import { fetchReportBlob } from "../api/reportsApi";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

export function useReportBlob(
  reportId: string,
  format: "pdf" | "xlsx" | "docx" | "excel" | "word",
  model?: ReportDocumentModel
) {
  return useQuery({
    queryKey: ["report-blob", reportId, format, model],
    queryFn: () => fetchReportBlob(reportId, format, model),
    enabled: Boolean(reportId || model)
  });
}
