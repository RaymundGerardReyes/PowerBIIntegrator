import React, { useRef, useState } from "react";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";
import { DataTable } from "@shared/ui/DataTable/DataTable";
import type { ColumnSchemaDto } from "@shared/types/api-contracts";

export const ExcelUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [extractedSchema, setExtractedSchema] = useState<ColumnSchemaDto[] | null>(null);
  const { mutate, isPending, data } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) {
      mutate(
        { file, type: "excel" },
        {
          onSuccess: (result) => {
            if (result.schema && result.schema.length > 0) {
              setExtractedSchema(result.schema);
            }
          }
        }
      );
    }
  };

  const activeSchema = extractedSchema ?? data?.schema ?? [];
  const schemaRows: Record<string, unknown>[] = activeSchema.map((col) => ({
    name: col.name,
    dataType: col.dataType,
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
        <input ref={inputRef} type="file" accept=".xlsx" aria-label="excel-upload-input" />
        <Button onClick={handleUpload} disabled={isPending}>
          {isPending ? "Uploading..." : "Upload Excel"}
        </Button>
      </div>

      {schemaRows.length > 0 && (
        <div style={{ marginTop: "12px" }}>
          <h4>Extracted Column Schema</h4>
          <DataTable columns={schemaColumns} rows={schemaRows} />
        </div>
      )}
    </div>
  );
};
