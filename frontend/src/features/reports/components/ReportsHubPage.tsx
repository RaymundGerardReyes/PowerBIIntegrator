import React, { useState } from "react";
import { PdfReportViewer } from "./PdfReportViewer";
import { ExcelReportPreview } from "./ExcelReportPreview";
import { WordReportViewer } from "./WordReportViewer";
import { Button } from "@shared/ui/Button/Button";
import { generateReportBlob, downloadBlob, type ReportFormat } from "../api/reportsApi";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

export const ReportsHubPage: React.FC = () => {
  const [selectedFormat, setSelectedFormat] = useState<ReportFormat>("pdf");
  const [isExporting, setIsExporting] = useState(false);

  // Editable report document model
  const [model, setModel] = useState<ReportDocumentModel>({
    title: "Executive Revenue & Analytics Report",
    subtitle: "Consolidated enterprise KPI & quarterly performance summary",
    author: "Enterprise Strategy Office",
    organization: "PowerBI Analytics Platform",
    sections: [
      {
        heading: "Q3 Performance Overview",
        narrative: "This report was generated server-side using multi-target document engines targeting vector PDF, OpenXML spreadsheet, and Word document formats.",
        kpis: [
          { title: "Net Revenue", value: "$4,850,000", subtitle: "vs prior quarter", deltaPercent: 14.2, isPositiveDelta: true },
          { title: "Operating Margin", value: "32.4%", subtitle: "target: 30%", deltaPercent: 2.4, isPositiveDelta: true }
        ],
        tableHeaders: ["Business Unit", "Actual Revenue", "Target Revenue", "Variance"],
        tableRows: [
          ["Enterprise Solutions", "$2,950,000", "$2,600,000", "+13.5%"],
          ["Cloud Analytics", "$1,400,000", "$1,250,000", "+12.0%"],
          ["Consulting & Support", "$500,000", "$480,000", "+4.2%"]
        ]
      }
    ]
  });

  const handleDownloadDirect = async (format: ReportFormat) => {
    setIsExporting(true);
    try {
      const blob = await generateReportBlob(format, model);
      const ext = format === "excel" ? "xlsx" : format === "word" ? "docx" : "pdf";
      downloadBlob(blob, `${model.title.replace(/\s+/g, "_")}.${ext}`);
    } catch (err) {
      console.error("Report generation failed:", err);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div>
        <h2 style={{ marginBottom: "0.25rem" }}>Executive Report Generation Hub</h2>
        <p style={{ margin: 0, color: "var(--text-secondary)" }}>
          Synthesize high-fidelity reports across PDF (QuestPDF), Excel (ClosedXML), and Word (DocumentFormat.OpenXml) directly from canonical analytical models.
        </p>
      </div>

      {/* Format Selector Pills & Export Action Bar */}
      <div className="card" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: "1rem" }}>
        <div style={{ display: "flex", gap: "0.5rem" }} role="group" aria-label="report-format-selector">
          {(["pdf", "excel", "word"] as const).map((fmt) => (
            <button
              key={fmt}
              onClick={() => setSelectedFormat(fmt)}
              className={`tab-item ${selectedFormat === fmt ? "tab-item-active" : ""}`}
              style={{ padding: "0.5rem 1.25rem", borderRadius: "9999px", textTransform: "uppercase", fontSize: "0.75rem", letterSpacing: "0.05em" }}
              aria-pressed={selectedFormat === fmt}
            >
              {fmt === "pdf" ? "PDF Document" : fmt === "excel" ? "Excel Workbook (.xlsx)" : "Word Document (.docx)"}
            </button>
          ))}
        </div>

        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button
            onClick={() => handleDownloadDirect(selectedFormat)}
            disabled={isExporting}
            aria-label="download-current-report"
          >
            {isExporting ? "Generating..." : `Download ${selectedFormat.toUpperCase()}`}
          </Button>
        </div>
      </div>

      {/* Metadata Configuration Drawer / Card */}
      <details className="card" style={{ cursor: "pointer" }}>
        <summary style={{ fontWeight: 600, color: "var(--primary)", userSelect: "none" }}>
          Customize Report Title & Organization Metadata
        </summary>
        <div style={{ marginTop: "1rem", display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: "1rem" }}>
          <div className="form-group">
            <label className="form-label">Report Title</label>
            <input
              className="form-input"
              value={model.title}
              onChange={(e) => setModel({ ...model, title: e.target.value })}
              aria-label="report-title-input"
            />
          </div>
          <div className="form-group">
            <label className="form-label">Report Subtitle</label>
            <input
              className="form-input"
              value={model.subtitle ?? ""}
              onChange={(e) => setModel({ ...model, subtitle: e.target.value })}
              aria-label="report-subtitle-input"
            />
          </div>
          <div className="form-group">
            <label className="form-label">Author</label>
            <input
              className="form-input"
              value={model.author ?? ""}
              onChange={(e) => setModel({ ...model, author: e.target.value })}
              aria-label="report-author-input"
            />
          </div>
          <div className="form-group">
            <label className="form-label">Organization</label>
            <input
              className="form-input"
              value={model.organization ?? ""}
              onChange={(e) => setModel({ ...model, organization: e.target.value })}
              aria-label="report-organization-input"
            />
          </div>
        </div>
      </details>

      {/* Live Preview Pane */}
      <div className="card">
        <h3 style={{ fontSize: "1.125rem", marginBottom: "1rem" }}>
          Live {selectedFormat.toUpperCase()} Preview
        </h3>

        {selectedFormat === "pdf" && <PdfReportViewer model={model} />}
        {selectedFormat === "excel" && <ExcelReportPreview model={model} />}
        {selectedFormat === "word" && <WordReportViewer model={model} />}
      </div>
    </div>
  );
};

