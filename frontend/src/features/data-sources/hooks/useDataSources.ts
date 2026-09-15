import { useMutation } from "@tanstack/react-query";
import { uploadFile, registerSqlConnection } from "../api/dataSourcesApi";

export function useUploadFile() {
  return useMutation({ mutationFn: ({ file, type }: { file: File; type: "excel" | "csv" }) => uploadFile(file, type) });
}

export function useRegisterSqlConnection() {
  return useMutation({ mutationFn: registerSqlConnection });
}
