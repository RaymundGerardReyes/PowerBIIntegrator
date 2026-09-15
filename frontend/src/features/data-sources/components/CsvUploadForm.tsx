import React, { useRef } from "react";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";

export const CsvUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const { mutate, isPending } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) mutate({ file, type: "csv" });
  };

  return (
    <div>
      <input ref={inputRef} type="file" accept=".csv" aria-label="csv-upload-input" />
      <Button onClick={handleUpload} disabled={isPending}>
        {isPending ? "Uploading..." : "Upload CSV"}
      </Button>
    </div>
  );
};
