import { useQuery } from "@tanstack/react-query";
import { getDashboardDefinition } from "../api/dashboardsApi";

export function useDashboardDefinition(id: string) {
  return useQuery({
    queryKey: ["dashboard", id],
    queryFn: () => getDashboardDefinition(id),
    enabled: !!id
  });
}
