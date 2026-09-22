import React, { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";
import { DataTable } from "@shared/ui/DataTable/DataTable";
import type { ColumnSchemaDto } from "@shared/types/api-contracts";

export const ExcelUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [extractedSchema, setExtractedSchema] = useState<ColumnSchemaDto[] | null>(null);
  const { mutate, isPending, data, error } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) {
      const isCsv = file.name.toLowerCase().endsWith(".csv");
      const type = isCsv ? "csv" : "excel";
      mutate(
        { file, type },
        {
          onSuccess: (result) => {
            if (result.schema && result.schema.length > 0) {
              setExtractedSchema(result.schema);
            }
            if (result.name) {
              localStorage.setItem("powerbi_active_model_name", result.name);
            }
            if (result.id) {
              localStorage.setItem("powerbi_active_dataset_id", result.id);
              localStorage.setItem("powerbi_active_model_id", result.id);
            }
          }
        }
      );
    }
  };

  const activeSchema = extractedSchema ?? data?.schema ?? [];
  const schemaRows: Record<string, unknown>[] = activeSchema.map((col) => ({
    name: col.name,
    dataType: col.inferredType ?? col.dataType ?? "String",
    isNullable: col.isNullable ? "Yes" : "No",
    sampleValues: col.sampleValues?.join(", ") ?? "None"
  }));

  const schemaColumns = [
    { key: "name" as const, header: "Column Name" },
    { key: "dataType" as const, header: "Inferred Type" },
    { key: "isNullable" as const, header: "Nullable" },
    { key: "sampleValues" as const, header: "Sample Values" }
  ];

  const getErrorMessage = (err: unknown): string | null => {
    if (!err) return null;
    if (typeof err === "object" && err !== null && "response" in err) {
      const response = (err as { response?: { data?: unknown } }).response;
      if (Array.isArray(response?.data) && response.data.length > 0) {
        return response.data.join(", ");
      }
      if (typeof response?.data === "string") {
        return response.data;
      }
      if (typeof response?.data === "object" && response?.data !== null && "title" in response.data) {
        return String((response.data as { title: unknown }).title);
      }
    }
    return err instanceof Error ? err.message : "Upload failed";
  };

  const errorMessage = getErrorMessage(error);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
        <input ref={inputRef} type="file" accept=".xlsx,.xls,.csv" aria-label="excel-upload-input" />
        <Button onClick={handleUpload} disabled={isPending}>
          {isPending ? "Uploading..." : "Upload File"}
        </Button>
      </div>

      {errorMessage && (
        <div style={{ color: "#ef4444", fontSize: "0.875rem" }} role="alert">
          {errorMessage}
        </div>
      )}

      {data && (
        <div
          style={{
            padding: "0.85rem 1rem",
            backgroundColor: "var(--primary-tint)",
            border: "1px solid var(--primary)",
            borderRadius: "var(--radius-sm)",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            flexWrap: "wrap",
            gap: "0.75rem"
          }}
        >
          <div>
            <div style={{ fontWeight: 600, color: "var(--text-primary)", fontSize: "0.9rem" }}>
              ✅ Successfully registered: {data.name}
            </div>
            <div style={{ fontSize: "0.8rem", color: "var(--text-secondary)" }}>
              Data source processed and semantic model ready for Power BI Desktop compilation.
            </div>
          </div>
          <Link
            to={`/dashboards?dataset=${encodeURIComponent(data.name)}`}
            className="btn btn-primary btn-sm"
            style={{ textDecoration: "none" }}
          >
            Open in Dashboards & Launch Power BI →
          </Link>
        </div>
      )}

      {schemaRows.length > 0 && (
        <div style={{ marginTop: "12px" }}>
          <h4>Extracted Column Schema</h4>
          <DataTable columns={schemaColumns} rows={schemaRows} />
        </div>
      )}
    </div>
  );
};
