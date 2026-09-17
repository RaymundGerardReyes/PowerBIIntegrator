import { useDashboardStore } from "../model/dashboardSlice";

export function useLayoutEditor() {
  const updateVisualLayout = useDashboardStore((s) => s.updateVisualLayout);
  const updateVisualType = useDashboardStore((s) => s.updateVisualType);
  const updateVisualTitle = useDashboardStore((s) => s.updateVisualTitle);
  const updateVisualBoundField = useDashboardStore((s) => s.updateVisualBoundField);
  return { updateVisualLayout, updateVisualType, updateVisualTitle, updateVisualBoundField };
}
