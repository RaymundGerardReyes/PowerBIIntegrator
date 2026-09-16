import React, { useState } from "react";
import { DataTable } from "@shared/ui/DataTable/DataTable";
import { Button } from "@shared/ui/Button/Button";
import { generateReportBlob, downloadBlob } from "../api/reportsApi";
import type { ReportDocumentModel } from "@shared/types/api-contracts";

interface ExcelReportPreviewProps {
  rows?: Record<string, unknown>[];
  model?: ReportDocumentModel;
}

export const ExcelReportPreview: React.FC<ExcelReportPreviewProps> = ({ rows = [], model }) => {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = async () => {
    if (!model) return;
    setIsExporting(true);
    try {
      const blob = await generateReportBlob("excel", model);
      downloadBlob(blob, `${model.title ?? "export"}.xlsx`);
    } finally {
      setIsExporting(false);
    }
  };

  // Convert model table rows to records if rows is empty but model has table
  const effectiveRows: Record<string, unknown>[] = rows.length > 0
    ? rows
    : (model?.sections.flatMap((sec) => {
        if (!sec.tableHeaders || !sec.tableRows) return [];
        return sec.tableRows.map((r) => {
          const rowObj: Record<string, unknown> = {};
          sec.tableHeaders?.forEach((h, idx) => {
            rowObj[h] = r[idx] ?? "";
          });
          return rowObj;
        });
      }) ?? []);

  if (effectiveRows.length === 0) return <p>No data.</p>;

  const columns = Object.keys(effectiveRows[0]).map((key) => ({
    key: key as keyof (typeof effectiveRows)[number],
    header: key
  }));

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "12px", width: "100%" }}>
      {model && (
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <div>
            <h3>{model.title}</h3>
            {model.subtitle && <p style={{ color: "#666", margin: 0 }}>{model.subtitle}</p>}
          </div>
          <Button onClick={handleExport} disabled={isExporting} aria-label="export-excel-button">
            {isExporting ? "Exporting..." : "Export to Excel (.xlsx)"}
          </Button>
        </div>
      )}
      <DataTable columns={columns} rows={effectiveRows} />
    </div>
  );
};
