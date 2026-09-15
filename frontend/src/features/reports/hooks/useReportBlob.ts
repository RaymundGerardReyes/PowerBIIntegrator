import { useQuery } from "@tanstack/react-query";
import { fetchReportBlob } from "../api/reportsApi";

export function useReportBlob(reportId: string, format: "pdf" | "xlsx" | "docx") {
  return useQuery({
    queryKey: ["report-blob", reportId, format],
    queryFn: () => fetchReportBlob(reportId, format),
    enabled: !!reportId
  });
}
