import React, { useState, useEffect } from "react";
import { PdfReportViewer } from "./PdfReportViewer";
import { ExcelReportPreview } from "./ExcelReportPreview";
import { WordReportViewer } from "./WordReportViewer";
import { Button } from "@shared/ui/Button/Button";
import { useDataSources, type DataSourceDefinition } from "@features/data-sources";
import { generateReportBlob, downloadBlob, type ReportFormat } from "../api/reportsApi";
import type { ReportDocumentModel, ColumnSchemaDto } from "@shared/types/api-contracts";

export const ReportsHubPage: React.FC = () => {
  const [selectedFormat, setSelectedFormat] = useState<ReportFormat>("pdf");
  const [isExporting, setIsExporting] = useState(false);
  const { data: dataSources = [] } = useDataSources();
  const [selectedSourceId, setSelectedSourceId] = useState<string>("");

  // Editable report document model
  const [model, setModel] = useState<ReportDocumentModel>({
    title: "Executive Revenue & Analytics Report",
    subtitle: "Consolidated enterprise KPI & dataset performance summary",
    author: "Enterprise Strategy Office",
    organization: "PowerBI Analytics Platform",
    sections: [
      {
        heading: "Analytical Overview",
        narrative: "This report was generated server-side using multi-target document engines targeting vector PDF, OpenXML spreadsheet, and Word document formats.",
        kpis: [
          { title: "Attributes", value: "0", subtitle: "Registered columns", deltaPercent: 100, isPositiveDelta: true },
          { title: "Quality Score", value: "100%", subtitle: "Contract compliance", deltaPercent: 0, isPositiveDelta: true }
        ],
        tableHeaders: ["Attribute", "Data Type", "Constraint", "Status"],
        tableRows: [
          ["DefaultDataset", "Canonical", "Standardized", "Active"]
        ]
      }
    ]
  });

  useEffect(() => {
    if (dataSources.length > 0) {
      const active = dataSources.find((ds: DataSourceDefinition) => ds.id === selectedSourceId) || dataSources[dataSources.length - 1];
      if (active) {
        if (!selectedSourceId) {
          setSelectedSourceId(active.id);
        }
        const schemaRows = active.schema && active.schema.length > 0
          ? active.schema.map((c: ColumnSchemaDto) => [
              c.name,
              c.inferredType ?? c.dataType ?? "String",
              c.isNullable ? "Nullable" : "Required",
              "Validated"
            ])
          : [[active.name, active.type.toUpperCase(), "Active", "Validated"]];

        setModel({
          title: `${active.name} Analytics Report`,
          subtitle: `Automated analytical synthesis & medallion profile summary for ${active.name}`,
          author: "Enterprise Strategy Office",
          organization: "PowerBI Analytics Platform",
          sections: [
            {
              heading: `${active.name} Schema & Distribution`,
              narrative: `This report synthesizes ${active.schema?.length ?? 0} attributes from ${active.name} (${active.type.toUpperCase()}) with verified data quality rules.`,
              kpis: [
                { title: "Total Attributes", value: String(active.schema?.length ?? 0), subtitle: "Registered fields", deltaPercent: 100, isPositiveDelta: true },
                { title: "Data Source Type", value: active.type.toUpperCase(), subtitle: "Engine connector", deltaPercent: 0, isPositiveDelta: true },
                { title: "Contract Compliance", value: "100%", subtitle: "Medallion verified", deltaPercent: 0, isPositiveDelta: true }
              ],
              tableHeaders: ["Attribute Name", "Inferred Type", "Nullability", "Governance Status"],
              tableRows: schemaRows
            }
          ]
        });
      }
    }
  }, [dataSources, selectedSourceId]);

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
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "1rem" }}>
        <div>
          <h2 style={{ marginBottom: "0.25rem" }}>Executive Report Generation Hub</h2>
          <p style={{ margin: 0, color: "var(--text-secondary)" }}>
            Synthesize high-fidelity reports across PDF (QuestPDF), Excel (ClosedXML), and Word (DocumentFormat.OpenXml) directly from canonical analytical models.
          </p>
        </div>

        {dataSources.length > 0 && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <label htmlFor="report-source-select" style={{ fontSize: "0.8125rem", fontWeight: 500, color: "var(--text-secondary)" }}>
              Data Source:
            </label>
            <select
              id="report-source-select"
              className="form-input"
              style={{ minWidth: "180px", padding: "0.375rem 0.75rem", fontSize: "0.8125rem" }}
              value={selectedSourceId}
              onChange={(e) => setSelectedSourceId(e.target.value)}
            >
              {dataSources.map((ds: DataSourceDefinition) => (
                <option key={ds.id} value={ds.id}>
                  {ds.name} ({ds.type.toUpperCase()})
                </option>
              ))}
            </select>
          </div>
        )}
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

