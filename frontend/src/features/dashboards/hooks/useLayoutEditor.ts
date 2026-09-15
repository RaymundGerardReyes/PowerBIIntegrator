import { useDashboardStore } from "../model/dashboardSlice";

export function useLayoutEditor() {
  const updateVisualLayout = useDashboardStore((s) => s.updateVisualLayout);
  return { updateVisualLayout };
}
