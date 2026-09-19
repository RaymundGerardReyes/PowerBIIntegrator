import React, { useState, useEffect } from "react";
import { PdfReportViewer } from "./PdfReportViewer";
import { ExcelReportPreview } from "./ExcelReportPreview";
import { WordReportViewer } from "./WordReportViewer";
import { Button, ContextBar, WorkflowStepper } from "@shared/ui";
import { useDataSources, type DataSourceDefinition } from "@features/data-sources";
import { generateReportBlob, downloadBlob, type ReportFormat } from "../api/reportsApi";
import type { ReportDocumentModel, ColumnSchemaDto } from "@shared/types/api-contracts";

export const ReportsHubPage: React.FC = () => {
  const [selectedFormat, setSelectedFormat] = useState<ReportFormat>("pdf");
  const [isExporting, setIsExporting] = useState(false);
  const [isConfigOpen, setIsConfigOpen] = useState(true);
  const { data: dataSources = [] } = useDataSources();
  const [selectedSourceId, setSelectedSourceId] = useState<string>("");

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

  const activeSource = dataSources.find((ds: DataSourceDefinition) => ds.id === selectedSourceId) || null;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <ContextBar
        title="Executive Report Generation Hub"
        metadata={[
          { label: "Data Source", value: activeSource ? activeSource.name : "None" },
          { label: "Target Format", value: selectedFormat.toUpperCase() }
        ]}
        secondaryAction={
          <select
            className="form-input"
            style={{ padding: "0.3rem 0.5rem", fontSize: "0.8rem", width: "160px", height: "30px" }}
            value={selectedSourceId}
            onChange={(e) => setSelectedSourceId(e.target.value)}
          >
            <option value="" disabled>Switch dataset...</option>
            {dataSources.map((ds: DataSourceDefinition) => (
              <option key={ds.id} value={ds.id}>{ds.name}</option>
            ))}
          </select>
        }
        primaryAction={
          <Button
            variant="primary"
            className="btn-sm"
            onClick={() => handleDownloadDirect(selectedFormat)}
            disabled={isExporting}
          >
            {isExporting ? "Generating..." : `Export ${selectedFormat.toUpperCase()}`}
          </Button>
        }
      />

      <WorkflowStepper
        steps={[
          { id: "pdf", label: "PDF Document", status: selectedFormat === "pdf" ? "active" : "pending" },
          { id: "excel", label: "Excel Workbook (.xlsx)", status: selectedFormat === "excel" ? "active" : "pending" },
          { id: "word", label: "Word Document (.docx)", status: selectedFormat === "word" ? "active" : "pending" }
        ]}
        onStepClick={(id) => setSelectedFormat(id as ReportFormat)}
      />

      {/* Metadata Configuration */}
      <div className="card">
        <div 
          style={{ display: "flex", justifyContent: "space-between", alignItems: "center", cursor: "pointer", userSelect: "none" }}
          onClick={() => setIsConfigOpen(!isConfigOpen)}
        >
          <h4 style={{ margin: 0, fontSize: "0.95rem" }}>Report Metadata Configuration</h4>
          <span style={{ fontSize: "0.875rem", color: "var(--text-secondary)" }}>
            {isConfigOpen ? "Collapse ▴" : "Expand ▾"}
          </span>
        </div>
        
        {isConfigOpen && (
          <div style={{ marginTop: "1rem", display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", borderTop: "1px solid var(--border-color)", paddingTop: "1rem" }}>
            <div className="form-group">
              <label className="form-label" htmlFor="report-title-input">Report Title</label>
              <input
                id="report-title-input"
                aria-label="report-title-input"
                className="form-input"
                value={model.title}
                onChange={(e) => setModel({ ...model, title: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Report Subtitle</label>
              <input
                className="form-input"
                value={model.subtitle ?? ""}
                onChange={(e) => setModel({ ...model, subtitle: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Author</label>
              <input
                className="form-input"
                value={model.author ?? ""}
                onChange={(e) => setModel({ ...model, author: e.target.value })}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Organization</label>
              <input
                className="form-input"
                value={model.organization ?? ""}
                onChange={(e) => setModel({ ...model, organization: e.target.value })}
              />
            </div>
          </div>
        )}
      </div>

      {/* Live Preview Pane */}
      <div className="card" style={{ minHeight: "500px" }}>
        <h3 style={{ fontSize: "1rem", marginBottom: "1rem" }}>
          Live {selectedFormat.toUpperCase()} Preview
        </h3>

        {selectedFormat === "pdf" && <PdfReportViewer model={model} />}
        {selectedFormat === "excel" && <ExcelReportPreview model={model} />}
        {selectedFormat === "word" && <WordReportViewer model={model} />}
      </div>
    </div>
  );
};

