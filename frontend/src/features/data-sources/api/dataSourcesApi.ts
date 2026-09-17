import { apiClient } from "@shared/lib/http/apiClient";
import type { DataSourceDefinition } from "../model/types";

export async function uploadFile(file: File, type: "excel" | "csv"): Promise<DataSourceDefinition> {
  const form = new FormData();
  form.append("file", file);
  form.append("type", type);
  const { data } = await apiClient.post<DataSourceDefinition>("/api/data-sources/upload", form);
  return data;
}

export async function registerSqlConnection(payload: {
  name: string;
  connectionString: string;
  type: "sqlserver" | "postgresql" | "mysql";
}): Promise<DataSourceDefinition> {
  const { data } = await apiClient.post<DataSourceDefinition>("/api/data-sources/sql", payload);
  return data;
}

export async function getDataSources(): Promise<DataSourceDefinition[]> {
  const { data } = await apiClient.get<DataSourceDefinition[]>("/api/data-sources");
  return data;
}

export async function getDataSourceSchema(id: string): Promise<import("@shared/types/api-contracts").ColumnSchemaDto[]> {
  const { data } = await apiClient.get<import("@shared/types/api-contracts").ColumnSchemaDto[]>(`/api/data-sources/${id}/schema`);
  return data;
}
