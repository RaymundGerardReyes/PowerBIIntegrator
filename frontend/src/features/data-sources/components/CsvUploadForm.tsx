import React, { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";
import { DataTable } from "@shared/ui/DataTable/DataTable";
import type { ColumnSchemaDto } from "@shared/types/api-contracts";

export const CsvUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [extractedSchema, setExtractedSchema] = useState<ColumnSchemaDto[] | null>(null);
  const { mutate, isPending, data } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) {
      mutate(
        { file, type: "csv" },
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

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
      <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
        <input ref={inputRef} type="file" accept=".csv" aria-label="csv-upload-input" />
        <Button onClick={handleUpload} disabled={isPending}>
          {isPending ? "Uploading..." : "Upload CSV"}
        </Button>
      </div>

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
