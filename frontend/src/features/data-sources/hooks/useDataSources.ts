import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { uploadFile, registerSqlConnection, getDataSources } from "../api/dataSourcesApi";

export function useDataSources() {
  return useQuery({
    queryKey: ["data-sources"],
    queryFn: getDataSources,
  });
}

export function useUploadFile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, type }: { file: File; type: "excel" | "csv" }) => uploadFile(file, type),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["data-sources"] });
    },
  });
}

export function useRegisterSqlConnection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registerSqlConnection,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["data-sources"] });
    },
  });
}
