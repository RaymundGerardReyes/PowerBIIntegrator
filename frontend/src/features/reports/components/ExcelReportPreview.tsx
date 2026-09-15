import React from "react";
import { DataTable } from "@shared/ui/DataTable/DataTable";

interface ExcelReportPreviewProps {
  rows: Record<string, unknown>[];
}

export const ExcelReportPreview: React.FC<ExcelReportPreviewProps> = ({ rows }) => {
  if (rows.length === 0) return <p>No data.</p>;
  const columns = Object.keys(rows[0]).map((key) => ({ key: key as keyof (typeof rows)[number], header: key }));
  return <DataTable columns={columns} rows={rows} />;
};
