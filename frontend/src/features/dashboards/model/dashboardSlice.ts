import { create } from "zustand";
import type { DashboardDefinition } from "./types";

interface DashboardState {
  current: DashboardDefinition | null;
  setDashboard: (dashboard: DashboardDefinition) => void;
  updateVisualLayout: (pageName: string, visualName: string, layout: Partial<import("@entities/visual/types").VisualLayout>) => void;
  updateVisualType: (pageName: string, visualName: string, visualType: string) => void;
  updateVisualTitle: (pageName: string, visualName: string, title: string) => void;
  updateVisualBoundField: (pageName: string, visualName: string, fieldIndex: number, newField: string) => void;
  addVisual: (pageName: string, visual: import("@entities/visual/types").Visual) => void;
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
    }),
  updateVisualType: (pageName, visualName, visualType) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, visualType }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualTitle: (pageName, visualName, name) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, name }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualBoundField: (pageName, visualName, fieldIndex, newField) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) => {
                if (v.name !== visualName) return v;
                const updated = [...v.boundFields];
                if (fieldIndex < updated.length) {
                  updated[fieldIndex] = newField;
                } else {
                  updated.push(newField);
                }
                return { ...v, boundFields: updated };
              })
            }
      );
      return { current: { ...state.current, pages } };
    }),
  addVisual: (pageName, visual) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: [...page.visuals, visual]
            }
      );
      return { current: { ...state.current, pages } };
    })
}));
