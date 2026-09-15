import { create } from "zustand";
import type { DashboardDefinition } from "./types";

interface DashboardState {
  current: DashboardDefinition | null;
  setDashboard: (dashboard: DashboardDefinition) => void;
  updateVisualLayout: (pageName: string, visualName: string, layout: Partial<import("@entities/visual/types").VisualLayout>) => void;
}

export const useDashboardStore = create<DashboardState>((set) => ({
  current: null,
  setDashboard: (dashboard) => set({ current: dashboard }),
  updateVisualLayout: (pageName, visualName, layout) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, layout: { ...v.layout, ...layout } }
              )
            }
      );
      return { current: { ...state.current, pages } };
    })
}));
