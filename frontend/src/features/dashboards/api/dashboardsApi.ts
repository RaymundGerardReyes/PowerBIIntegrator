import { apiClient } from "@shared/lib/http/apiClient";
import type { DashboardDefinition } from "../model/types";

export async function getDashboardDefinition(id: string): Promise<DashboardDefinition> {
  const { data } = await apiClient.get<DashboardDefinition>(`/api/dashboards/${id}`);
  return data;
}

export async function saveDashboardDefinition(dashboard: DashboardDefinition): Promise<void> {
  await apiClient.put(`/api/dashboards/${dashboard.id}`, dashboard);
}
