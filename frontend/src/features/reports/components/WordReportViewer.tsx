import React, { useState } from "react";
import { Button } from "@shared/ui/Button/Button";
import { generateReportBlob, downloadBlob } from "../api/reportsApi";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

interface WordReportViewerProps {
  reportId?: string;
  model?: ReportDocumentModel;
}

export const WordReportViewer: React.FC<WordReportViewerProps> = ({ reportId, model }) => {
  const [isExporting, setIsExporting] = useState(false);

  const handleDownload = async () => {
    if (!model) return;
    setIsExporting(true);
    try {
      const blob = await generateReportBlob("word", model);
      downloadBlob(blob, `${model.title ?? "document"}.docx`);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "12px", width: "100%" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h3>Word Document Report {reportId ? `(${reportId})` : ""}</h3>
          <p style={{ color: "#666", margin: 0 }}>
            Generated server-side via OpenXML (`DocumentFormat.OpenXml`).
          </p>
        </div>
        {model && (
          <Button onClick={handleDownload} disabled={isExporting} aria-label="export-word-button">
            {isExporting ? "Exporting..." : "Export to Word (.docx)"}
          </Button>
        )}
      </div>
      {model && (
        <div style={{ border: "1px solid #e0e0e0", padding: "16px", borderRadius: "6px", background: "#fafafa" }}>
          <h4>{model.title}</h4>
          {model.subtitle && <p style={{ fontStyle: "italic" }}>{model.subtitle}</p>}
          {model.sections.map((section, idx) => (
            <div key={idx} style={{ marginTop: "12px" }}>
              <h5>{section.heading}</h5>
              {section.narrative && <p>{section.narrative}</p>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
