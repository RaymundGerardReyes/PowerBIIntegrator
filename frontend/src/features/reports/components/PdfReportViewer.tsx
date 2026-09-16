import React, { useEffect, useState } from "react";
import { useReportBlob } from "../hooks/useReportBlob";
import { downloadBlob } from "../api/reportsApi";
import { Button } from "@shared/ui/Button/Button";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

interface PdfReportViewerProps {
  reportId?: string;
  model?: ReportDocumentModel;
}

export const PdfReportViewer: React.FC<PdfReportViewerProps> = ({ reportId = "", model }) => {
  const { data, isLoading, error } = useReportBlob(reportId, "pdf", model);
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (data && typeof URL !== "undefined" && typeof URL.createObjectURL === "function") {
      const objectUrl = URL.createObjectURL(data);
      setUrl(objectUrl);
      return () => {
        if (typeof URL.revokeObjectURL === "function") {
          URL.revokeObjectURL(objectUrl);
        }
      };
    }
  }, [data]);

  const handleDownload = () => {
    if (data) {
      downloadBlob(data, `${model?.title ?? "report"}.pdf`);
    }
  };

  if (isLoading) return <p>Generating PDF report...</p>;
  if (error) return <p>Error generating report: {(error as Error).message}</p>;
  if (!url) return <p>No PDF loaded.</p>;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "12px", width: "100%" }}>
      <div style={{ display: "flex", justifyContent: "flex-end" }}>
        <Button onClick={handleDownload} aria-label="download-pdf-button">
          Download PDF
        </Button>
      </div>
      <iframe title="pdf-report" src={url} style={{ width: "100%", height: "80vh", border: "1px solid #ccc", borderRadius: "4px" }} />
    </div>
  );
};
